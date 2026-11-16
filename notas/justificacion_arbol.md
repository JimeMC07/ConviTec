# Justificación: árbol AVL para el catálogo de productos

## Qué necesitamos
Cada sede consulta su catálogo constantemente: al registrar un pedido, al reservar,
al confirmar, al agregar unidades y al responder mensajes de otras sedes.
La operación más frecuente es **buscar un producto por su código**.
También se necesita **listar todo el catálogo ordenado** (para la interfaz)
e **insertar** productos nuevos.

## Alternativas consideradas

| Estructura | Buscar | Insertar | Eliminar | Listar ordenado | Problema |
|---|---|---|---|---|---|
| Lista enlazada | O(n) | O(1) | O(n) | O(n log n) (hay que ordenar) | Buscar es lento: revisa uno por uno |
| Arreglo ordenado | O(log n) | O(n) | O(n) | O(n) | Insertar obliga a correr todos los elementos |
| Árbol binario de búsqueda (BST) | O(log n) promedio, **O(n) peor caso** | igual | igual | O(n) | Si los códigos llegan en orden (P001, P002...) se degenera en una lista |
| **Árbol AVL** | **O(log n) siempre** | **O(log n)** | **O(log n)** | **O(n)** (recorrido en orden) | Código un poco más complejo (rotaciones) |
| Árbol B | O(log n) | O(log n) | O(log n) | O(n) | Pensado para disco/bases de datos; demasiado complejo para catálogos en memoria |

## Decisión: árbol AVL
1. **Garantiza O(log n) en el peor caso.** Los códigos de producto suelen ser
   secuenciales (P001, P002, P003...). Un BST normal con esos datos se vuelve
   una línea recta (O(n)); el AVL se rebalancea con rotaciones y se mantiene bajo.
   En nuestras pruebas, 100 productos insertados en orden dejan el árbol con altura 7
   (en un BST normal serían 100 niveles).
2. **El recorrido en orden** devuelve el catálogo ya ordenado por código, sin
   necesidad de un algoritmo de ordenamiento aparte.
3. **No permite duplicados** de forma natural: si el código ya existe, la inserción
   se rechaza.
4. Es más simple que un árbol B y suficiente para catálogos que caben en memoria.

## Dónde se usa
- `InventarioLocal`: clave = código del producto, valor = `ItemInventario`
  (producto + disponible + reservado).
- `InventarioGlobal`: clave = código del producto, valor = `EntradaCatalogo`
  (producto + existencias por sede).
- Implementación: `ConviTEC.Core/Estructuras/ArbolAVL.cs`.