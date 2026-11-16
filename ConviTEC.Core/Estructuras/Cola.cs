using System.Collections;

namespace ConviTEC.Core.Estructuras
{
    /// <summary>
    /// Cola (FIFO) implementada por nosotros con nodos enlazados.
    /// El primero que entra es el primero que sale.
    /// </summary>
    public class Cola<T> : IEnumerable<T>
    {
        private Nodo<T>? frente;   // el primero de la fila (sale primero)
        private Nodo<T>? final;    // el último de la fila (entra aquí)

        public int Cantidad { get; private set; }

        public Cola()
        {
            frente = null;
            final = null;
            Cantidad = 0;
        }

        // ---- ¿Está vacía? ----
        public bool EstaVacia()
        {
            return Cantidad == 0;
        }

        // ---- Meter al final ----
        public void Encolar(T dato)
        {
            Nodo<T> nuevo = new Nodo<T>(dato);

            if (final == null)        // cola vacía
            {
                frente = nuevo;
                final = nuevo;
            }
            else
            {
                final.Siguiente = nuevo;
                final = nuevo;
            }
            Cantidad++;
        }

        // ---- Sacar el del frente ----
        public T Desencolar()
        {
            if (frente == null)
                throw new InvalidOperationException("La cola está vacía, no hay nada que sacar.");

            T dato = frente.Dato;
            frente = frente.Siguiente;

            if (frente == null)       // se quedó vacía
                final = null;

            Cantidad--;
            return dato;
        }

        // ---- Ver el del frente sin sacarlo ----
        public T VerFrente()
        {
            if (frente == null)
                throw new InvalidOperationException("La cola está vacía.");
            return frente.Dato;
        }

        // ---- Sacar un elemento específico (aunque no esté al frente) ----
        // Devuelve true si lo encontró y lo sacó.
        public bool Eliminar(Func<T, bool> condicion)
        {
            Nodo<T>? anterior = null;
            Nodo<T>? actual = frente;

            while (actual != null)
            {
                if (condicion(actual.Dato))
                {
                    if (anterior == null) frente = actual.Siguiente;
                    else anterior.Siguiente = actual.Siguiente;

                    if (actual == final) final = anterior;

                    Cantidad--;
                    return true;
                }
                anterior = actual;
                actual = actual.Siguiente;
            }
            return false;
        }

        // ---- Buscar sin sacar ----
        public T? Buscar(Func<T, bool> condicion)
        {
            Nodo<T>? actual = frente;
            while (actual != null)
            {
                if (condicion(actual.Dato))
                    return actual.Dato;
                actual = actual.Siguiente;
            }
            return default;
        }

        // ---- Vaciar ----
        public void Limpiar()
        {
            frente = null;
            final = null;
            Cantidad = 0;
        }

        // ---- Recorrer con foreach (del frente al final) ----
        public IEnumerator<T> GetEnumerator()
        {
            Nodo<T>? actual = frente;
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