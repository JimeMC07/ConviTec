using System.Collections;

namespace ConviTEC.Core.Estructuras
{
    /// <summary>
    /// Un nodo de la lista: guarda un dato y apunta al siguiente nodo.
    /// </summary>
    public class Nodo<T>
    {
        public T Dato { get; set; }
        public Nodo<T>? Siguiente { get; set; }

        public Nodo(T dato)
        {
            Dato = dato;
            Siguiente = null;
        }
    }

    /// <summary>
    /// Lista enlazada simple implementada por nosotros
    /// (no usa List<T> ni ninguna colección de C#).
    /// </summary>
    public class Lista<T> : IEnumerable<T>
    {
        private Nodo<T>? cabeza;   // primer nodo
        private Nodo<T>? cola;     // último nodo

        public int Cantidad { get; private set; }

        public Lista()
        {
            cabeza = null;
            cola = null;
            Cantidad = 0;
        }

        // ---- ¿Está vacía? ----
        public bool EstaVacia()
        {
            return Cantidad == 0;
        }

        // ---- Agregar al final ----
        public void Agregar(T dato)
        {
            Nodo<T> nuevo = new Nodo<T>(dato);

            if (cabeza == null)          // lista vacía: el nuevo es cabeza y cola
            {
                cabeza = nuevo;
                cola = nuevo;
            }
            else                         // enganchar después de la cola
            {
                cola!.Siguiente = nuevo;
                cola = nuevo;
            }
            Cantidad++;
        }

        // ---- Agregar al inicio ----
        public void AgregarAlInicio(T dato)
        {
            Nodo<T> nuevo = new Nodo<T>(dato);
            nuevo.Siguiente = cabeza;
            cabeza = nuevo;
            if (cola == null) cola = nuevo;
            Cantidad++;
        }

        // ---- Obtener el dato en una posición (0, 1, 2...) ----
        public T Obtener(int indice)
        {
            if (indice < 0 || indice >= Cantidad)
                throw new IndexOutOfRangeException($"Índice {indice} fuera de rango. La lista tiene {Cantidad} elementos.");

            Nodo<T> actual = cabeza!;
            for (int i = 0; i < indice; i++)
                actual = actual.Siguiente!;

            return actual.Dato;
        }

        // ---- Buscar el primer elemento que cumpla una condición ----
        // Ejemplo: lista.Buscar(p => p.Codigo == "P001")
        // Devuelve el valor por defecto (null) si no lo encuentra.
        public T? Buscar(Func<T, bool> condicion)
        {
            Nodo<T>? actual = cabeza;
            while (actual != null)
            {
                if (condicion(actual.Dato))
                    return actual.Dato;
                actual = actual.Siguiente;
            }
            return default;
        }

        // ---- ¿Existe algún elemento que cumpla la condición? ----
        public bool Existe(Func<T, bool> condicion)
        {
            Nodo<T>? actual = cabeza;
            while (actual != null)
            {
                if (condicion(actual.Dato))
                    return true;
                actual = actual.Siguiente;
            }
            return false;
        }

        // ---- Eliminar el primer elemento que cumpla la condición ----
        // Devuelve true si eliminó algo, false si no lo encontró.
        public bool Eliminar(Func<T, bool> condicion)
        {
            Nodo<T>? anterior = null;
            Nodo<T>? actual = cabeza;

            while (actual != null)
            {
                if (condicion(actual.Dato))
                {
                    if (anterior == null)            // era la cabeza
                        cabeza = actual.Siguiente;
                    else                             // el anterior se "salta" al actual
                        anterior.Siguiente = actual.Siguiente;

                    if (actual == cola)              // era la cola
                        cola = anterior;

                    Cantidad--;
                    return true;
                }
                anterior = actual;
                actual = actual.Siguiente;
            }
            return false;
        }

        // ---- Eliminar en una posición ----
        public T EliminarEn(int indice)
        {
            if (indice < 0 || indice >= Cantidad)
                throw new IndexOutOfRangeException($"Índice {indice} fuera de rango. La lista tiene {Cantidad} elementos.");

            Nodo<T>? anterior = null;
            Nodo<T> actual = cabeza!;
            for (int i = 0; i < indice; i++)
            {
                anterior = actual;
                actual = actual.Siguiente!;
            }

            if (anterior == null) cabeza = actual.Siguiente;
            else anterior.Siguiente = actual.Siguiente;

            if (actual == cola) cola = anterior;

            Cantidad--;
            return actual.Dato;
        }

        // ---- Vaciar la lista ----
        public void Limpiar()
        {
            cabeza = null;
            cola = null;
            Cantidad = 0;
        }

        // ---- Pasar a un arreglo nativo (permitido por el enunciado) ----
        public T[] ToArray()
        {
            T[] arreglo = new T[Cantidad];
            Nodo<T>? actual = cabeza;
            int i = 0;
            while (actual != null)
            {
                arreglo[i] = actual.Dato;
                i++;
                actual = actual.Siguiente;
            }
            return arreglo;
        }

        // ---- Permite recorrer la lista con foreach ----
        public IEnumerator<T> GetEnumerator()
        {
            Nodo<T>? actual = cabeza;
            while (actual != null)
            {
                yield return actual.Dato;
                actual = actual.Siguiente;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}