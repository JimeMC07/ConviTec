# ConviTec
Proyecto 2 CE1103 – Inventario distribuido y sistema de pedidos

## Preparación

Instalar .NET SDK 10, Git y Docker Desktop con el motor Linux. En PowerShell, desde la raíz del clon:

```powershell
docker desktop start
docker run -d --name convitec-rabbit -p 127.0.0.1:5672:5672 rabbitmq:4-management
docker exec convitec-rabbit rabbitmq-diagnostics -q ping
dotnet restore ConviTEC.slnx
dotnet build ConviTEC.slnx
dotnet run --project ConviTEC.Pruebas
```

Si el contenedor local ya existe, usar `docker start convitec-rabbit`. Esperar a que el diagnóstico responda antes de ejecutar las pruebas. Revisar que haya cero fallidas y ninguna línea OMITIDA.

## Cuatro sedes en dos computadoras

Ambos equipos necesitan el repositorio, el SDK y los tres JSON originales de `datos/`. Ejecutar un solo proceso por código de sede. Cada proceso crea su propio subdirectorio de datos. No compartirlo por red.

En el equipo A, para la demostración LAN, usar un broker accesible desde B. Si está corriendo el broker de pruebas local, detenerlo con `docker stop convitec-rabbit` para liberar 5672. En PowerShell 7 ejecutar:

```powershell
$env:RABBITMQ_DEFAULT_USER = 'convitec'
$env:RABBITMQ_DEFAULT_PASS = Read-Host 'Clave del broker para la demostración' -MaskInput
docker run -d --name convitec-lan -p 5672:5672 -e RABBITMQ_DEFAULT_USER -e RABBITMQ_DEFAULT_PASS rabbitmq:4-management
docker exec convitec-lan rabbitmq-diagnostics -q ping
```

Permitir TCP 5672 desde el equipo B en el firewall de A. En **cada terminal de sede**, configurar el mismo usuario, clave y dirección LAN de A (sustituir el ejemplo):

```powershell
$env:Rabbit__Host = '192.168.1.20'
$env:Rabbit__Usuario = 'convitec'
$env:Rabbit__Clave = Read-Host 'Clave del broker' -MaskInput
```

Abrir una terminal por fila y ejecutar el comando desde la raíz. En una prueba local con el broker de loopback, abrir terminales nuevas y usar los valores predeterminados de RabbitMQ.

| Equipo | Sede | Comando |
|---|---|---|
| A | Liberia | `dotnet run --project ConviTEC.Web -- --Sede S1 --urls http://localhost:5101` |
| A | Alajuela | `dotnet run --project ConviTEC.Web -- --Sede S2 --urls http://localhost:5102` |
| B | San José | `dotnet run --project ConviTEC.Web -- --Sede S3 --urls http://localhost:5103` |
| B | Limón | `dotnet run --project ConviTEC.Web -- --Sede S4 --urls http://localhost:5104` |

Abrir la dirección del comando en el navegador del equipo correspondiente. La sede queda fijada al iniciar el backend. El catálogo se sincroniza a través de RabbitMQ. El directorio de datos se puede configurar con `--Datos RUTA_ABSOLUTA`.

## Operaciones y demostración

En San José, registrar un pedido a Heredia y dos a Cartago. Pulsar **Calcular siguiente punto** dos veces. La pantalla muestra Heredia a los 20 minutos y Cartago a los 85 minutos acumulados. Revisar las tres ventas y sus productos en el historial. En Liberia, un pedido a Cartago debe mostrar el motivo de rechazo. Las salidas automáticas se ejecutan a las 11:00 y 15:00 de Costa Rica mientras la web está abierta. El botón también permite iniciar un lote manual para la defensa.

| Método | Ruta | Uso |
|---|---|---|
| GET | /api/sede, /api/destinos | Identidad local y ubicaciones |
| GET | /api/catalogo, /api/inventario | Existencias globales y locales |
| POST | /api/inventario/{codigo}/unidades | Cuerpo JSON `{"cantidad":3}` |
| POST | /api/pedidos | `{"destino":"Heredia","lineas":[{"codigoProducto":"P001","cantidad":2}]}` |
| GET | /api/pedidos, /api/pedidos/pendientes | Historial y pendientes |
| GET | /api/ventas | Detalle de ventas de la sede |
| GET | /api/estadisticas/estrella, /api/estadisticas/peores | Ingresos y rendimientos |
| GET | /api/entregas | Estado y tiempos del recorrido más reciente |
| POST | /api/entregas/siguiente | Entregar todos los pedidos de la próxima parada |

El [diagrama de clases](notas/clases.md) acompaña el [documento DI1–DI4](notas/diseno.md) y la [justificación del AVL](notas/justificacion_arbol.md).
