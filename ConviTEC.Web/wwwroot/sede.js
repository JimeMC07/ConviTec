const vista = document.querySelector("#vista");
const aviso = document.querySelector("#aviso");
const dinero = n => new Intl.NumberFormat("es-CR",{style:"currency",currency:"CRC"}).format(n);
const fecha = s => new Date(s).toLocaleString("es-CR");
const detalle = lineas => lineas.map(l => l.nombreProducto + " × " + l.cantidad + " a " + dinero(l.precioUnitario)).join(" · ");
function elemento(tipo, texto) {
    const nodo = document.createElement(tipo);
    if (texto !== undefined) nodo.textContent = texto;
    return nodo;
}
async function api(ruta, cuerpo) {
    const opciones = cuerpo === undefined ? {} : {method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify(cuerpo)};
    const respuesta = await fetch("/api/" + ruta, opciones);
    const dato = await respuesta.json();
    if (!respuesta.ok) throw Error(dato.mensaje || "No se pudo completar la operación.");
    return dato;
}
function tabla(columnas, filas) {
    if (!filas.length) return elemento("p","Sin registros.");
    const tabla = elemento("table");
    const cabeza = tabla.createTHead().insertRow();
    columnas.forEach(c => cabeza.append(elemento("th", c)));
    const cuerpo = tabla.createTBody();
    for (const fila of filas) {
        const tr = cuerpo.insertRow();
        for (const valor of fila) tr.insertCell().append(valor instanceof Node ? valor : String(valor));
    }
    return tabla;
}
function boton(texto, accion) {
    const b = elemento("button", texto);
    b.onclick = async () => {
        b.disabled = true;
        try { await accion(); } catch (e) { aviso.textContent = e.message; }
        finally { b.disabled = false; }
    };
    return b;
}
function cantidad(etiqueta, minimo) {
    const n = elemento("input");
    n.type = "number"; n.min = minimo; n.step = 1; n.value = minimo;
    n.setAttribute("aria-label", etiqueta);
    return n;
}
async function pedido(panel) {
    const [catalogo,destinos] = await Promise.all([api("catalogo"),api("destinos")]);
    const entradas = catalogo.map(e => cantidad(e.producto.nombre,0));
    panel.append(tabla(["Producto","Precio","Disponible","Unidades"],catalogo.map((e,i) =>
        [e.producto.nombre,dinero(e.producto.precio),e.totalGlobal,entradas[i]])));
    const etiqueta = elemento("label","Destino ");
    const select = elemento("select");
    destinos.forEach(d => select.add(new Option(d,d)));
    etiqueta.append(select);
    panel.append(etiqueta, boton("Registrar pedido", async () => {
        const lineas = [];
        for (const [i,n] of entradas.entries()) {
            if (!n.checkValidity() || !Number.isInteger(n.valueAsNumber)) throw Error("Ingresá cantidades enteras desde cero.");
            if (n.valueAsNumber) lineas.push({codigoProducto:catalogo[i].producto.codigo,cantidad:n.valueAsNumber});
        }
        const r = await api("pedidos",{destino:select.value,lineas});
        aviso.textContent = r.mensaje;
        await mostrar("Realizar pedido",true);
    }));
}
async function inventario(panel) {
    const datos = await api("inventario");
    panel.append(tabla(["Código","Producto","Precio","Disponibles","Reposición"],datos.map(i => {
        const grupo = elemento("div");
        const n = cantidad("Agregar unidades de " + i.producto.nombre,1);
        grupo.append(n,boton("Agregar",async () => {
            if (!n.checkValidity()) throw Error("Ingresá una cantidad positiva.");
            const r = await api("inventario/" + encodeURIComponent(i.producto.codigo) + "/unidades",{cantidad:n.valueAsNumber});
            aviso.textContent = r.mensaje;
            await mostrar("Inventario local",true);
        }));
        return [i.producto.codigo,i.producto.nombre,dinero(i.producto.precio),i.disponible,grupo];
    })));
}
async function historial(panel, pendientes) {
    const datos = await api(pendientes ? "pedidos/pendientes" : "pedidos");
    panel.append(tabla(["Pedido","Fecha","Destino","Detalle","Estado","Monto"],datos.map(p =>
        [p.id,fecha(p.fechaCreacion),p.destino,detalle(p.lineas),p.estado + (p.motivo ? " · " + p.motivo : ""),dinero(p.montoTotal)])));
}
async function rutas(panel) {
    const r = await api("entregas");
    panel.append(elemento("p","Origen: " + r.origen + " · Tiempo recorrido: " + r.minutosTotales + " min"),
        boton("Calcular siguiente punto",async () => {
            const paso = await api("entregas/siguiente",{});
            aviso.textContent = paso.visitadas ? "Última parada: " + paso.paradas[paso.visitadas-1].destino : "Sin pedidos pendientes.";
            await mostrar("Rutas de entrega",true);
        }),tabla(["Trayecto","Pedidos","Tramo","Acumulado","Estado"],r.paradas.map((p,i) =>
            [p.camino.join(" → "),p.pedidos.join(", "),p.minutosTramo+" min",p.minutosAcumulados+" min",i<r.visitadas?"Entregado":"Pendiente"])));
}
const pantallas = {
    "Realizar pedido":pedido, "Inventario local":inventario,
    "Catálogo global":async p => p.append(tabla(["Producto","Precio","Disponibilidad por sede"],(await api("catalogo")).map(e =>
        [e.producto.nombre,dinero(e.producto.precio),e.existencias.map(s=>s.codigoSede+" · "+s.cantidad).join(" / ")]))),
    "Pedidos pendientes":p=>historial(p,true),"Historial de pedidos":p=>historial(p,false),
    "Historial de ventas":async p=>p.append(tabla(["Pedido","Fecha","Productos","Monto"],(await api("ventas")).map(v=>
        [v.idPedido,fecha(v.fecha),detalle(v.lineas),dinero(v.montoTotal)]))),
    "Estadísticas":async p=>{
        p.append(elemento("h2","Producto estrella"),tabla(["Producto","Ingresos"],(await api("estadisticas/estrella")).map(e=>[e.nombre,dinero(e.ingresos)])),
            elemento("h2","Peores pedidos"),tabla(["Pedido","Monto","Minutos","₡ por minuto"],(await api("estadisticas/peores")).map(e=>
                [e.id,dinero(e.monto),e.minutos,e.rendimiento===null?"Sin traslado":dinero(e.rendimiento)])));
    },"Rutas de entrega":rutas
};
async function mostrar(nombre, conservar=false) {
    const panel = elemento("div");
    vista.replaceChildren(panel);
    document.querySelector("h1").textContent = nombre;
    if (!conservar) aviso.textContent="";
    for (const b of document.querySelectorAll("nav button")) b.setAttribute("aria-current",b.textContent===nombre);
    await pantallas[nombre](panel);
}
for (const nombre of Object.keys(pantallas)) document.querySelector("nav").append(boton(nombre,()=>mostrar(nombre)));
try {
    document.querySelector("#sede").textContent = (await api("sede")).nombre;
    await mostrar("Realizar pedido");
} catch (e) { aviso.textContent = e.message; }
