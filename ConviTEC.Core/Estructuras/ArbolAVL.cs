namespace ConviTEC.Core.Estructuras
{
    /// <summary>
    /// Nodo del árbol AVL: guarda una clave (ej. código de producto),
    /// el valor (ej. el Producto), sus dos hijos y su altura.
    /// </summary>
    public class NodoAVL<T>
    {
        public string Clave { get; set; }
        public T Valor { get; set; }
        public NodoAVL<T>? Izquierdo { get; set; }
        public NodoAVL<T>? Derecho { get; set; }
        public int Altura { get; set; }

        public NodoAVL(string clave, T valor)
        {
            Clave = clave;
            Valor = valor;
            Izquierdo = null;
            Derecho = null;
            Altura = 1;   // un nodo nuevo es una hoja: altura 1
        }
    }

    /// <summary>
    /// Árbol AVL implementado por nosotros.
    /// Mantiene el árbol balanceado para que buscar, insertar y eliminar sean O(log n).
    /// </summary>
    public class ArbolAVL<T>
    {
        private NodoAVL<T>? raiz;

        public int Cantidad { get; private set; }

        public ArbolAVL()
        {
            raiz = null;
            Cantidad = 0;
        }

        public bool EstaVacio()
        {
            return raiz == null;
        }

        // =========================================================
        //  MÉTODOS PÚBLICOS
        // =========================================================

        // ---- Insertar ----
        // Devuelve false si la clave ya existía (no se permiten duplicados).
        public bool Insertar(string clave, T valor)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new ArgumentException("La clave no puede estar vacía.");

            bool insertado = false;
            raiz = InsertarRecursivo(raiz, clave.Trim(), valor, ref insertado);
            if (insertado) Cantidad++;
            return insertado;
        }

        // ---- Buscar ----
        // Devuelve el valor, o null si no existe.
        public T? Buscar(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return default;

            NodoAVL<T>? actual = raiz;
            clave = clave.Trim();

            while (actual != null)
            {
                int comparacion = string.CompareOrdinal(clave, actual.Clave);
                if (comparacion == 0) return actual.Valor;           // encontrado
                actual = comparacion < 0 ? actual.Izquierdo          // menor → izquierda
                                         : actual.Derecho;           // mayor → derecha
            }
            return default;   // no existe
        }

        // ---- ¿Existe la clave? ----
        public bool Contiene(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return false;

            NodoAVL<T>? actual = raiz;
            clave = clave.Trim();
            while (actual != null)
            {
                int comparacion = string.CompareOrdinal(clave, actual.Clave);
                if (comparacion == 0) return true;
                actual = comparacion < 0 ? actual.Izquierdo : actual.Derecho;
            }
            return false;
        }

        // ---- Eliminar ----
        // Devuelve false si la clave no existía.
        public bool Eliminar(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return false;

            bool eliminado = false;
            raiz = EliminarRecursivo(raiz, clave.Trim(), ref eliminado);
            if (eliminado) Cantidad--;
            return eliminado;
        }

        // ---- Recorrido en orden (de menor a mayor clave) ----
        // Sirve para listar todo el catálogo ordenado por código.
        public Lista<T> EnOrden()
        {
            Lista<T> resultado = new Lista<T>();
            EnOrdenRecursivo(raiz, resultado);
            return resultado;
        }

        // ---- Altura total del árbol (útil para pruebas) ----
        public int AlturaArbol()
        {
            return Altura(raiz);
        }

        // =========================================================
        //  MÉTODOS PRIVADOS (la "magia" del AVL)
        // =========================================================

        private NodoAVL<T> InsertarRecursivo(NodoAVL<T>? nodo, string clave, T valor, ref bool insertado)
        {
            // Llegamos a un espacio vacío: aquí va el nuevo nodo
            if (nodo == null)
            {
                insertado = true;
                return new NodoAVL<T>(clave, valor);
            }

            int comparacion = string.CompareOrdinal(clave, nodo.Clave);

            if (comparacion < 0)
                nodo.Izquierdo = InsertarRecursivo(nodo.Izquierdo, clave, valor, ref insertado);
            else if (comparacion > 0)
                nodo.Derecho = InsertarRecursivo(nodo.Derecho, clave, valor, ref insertado);
            else
                return nodo;   // clave duplicada: no se inserta

            return Balancear(nodo);
        }

        private NodoAVL<T>? EliminarRecursivo(NodoAVL<T>? nodo, string clave, ref bool eliminado)
        {
            if (nodo == null) return null;   // no se encontró

            int comparacion = string.CompareOrdinal(clave, nodo.Clave);

            if (comparacion < 0)
                nodo.Izquierdo = EliminarRecursivo(nodo.Izquierdo, clave, ref eliminado);
            else if (comparacion > 0)
                nodo.Derecho = EliminarRecursivo(nodo.Derecho, clave, ref eliminado);
            else
            {
                eliminado = true;

                // Caso 1 y 2: tiene 0 o 1 hijo → se reemplaza por ese hijo
                if (nodo.Izquierdo == null) return nodo.Derecho;
                if (nodo.Derecho == null) return nodo.Izquierdo;

                // Caso 3: tiene 2 hijos → se reemplaza por el menor de la derecha (sucesor)
                NodoAVL<T> sucesor = nodo.Derecho;
                while (sucesor.Izquierdo != null)
                    sucesor = sucesor.Izquierdo;

                nodo.Clave = sucesor.Clave;
                nodo.Valor = sucesor.Valor;

                bool ignorar = false;
                nodo.Derecho = EliminarRecursivo(nodo.Derecho, sucesor.Clave, ref ignorar);
            }

            return Balancear(nodo);
        }

        private void EnOrdenRecursivo(NodoAVL<T>? nodo, Lista<T> resultado)
        {
            if (nodo == null) return;
            EnOrdenRecursivo(nodo.Izquierdo, resultado);   // primero los menores
            resultado.Agregar(nodo.Valor);                 // luego este
            EnOrdenRecursivo(nodo.Derecho, resultado);     // luego los mayores
        }

        // ---- Altura de un nodo (null = 0) ----
        private int Altura(NodoAVL<T>? nodo)
        {
            return nodo == null ? 0 : nodo.Altura;
        }

        private void ActualizarAltura(NodoAVL<T> nodo)
        {
            nodo.Altura = 1 + Math.Max(Altura(nodo.Izquierdo), Altura(nodo.Derecho));
        }

        // ---- Factor de balance = altura izquierda - altura derecha ----
        private int FactorBalance(NodoAVL<T>? nodo)
        {
            return nodo == null ? 0 : Altura(nodo.Izquierdo) - Altura(nodo.Derecho);
        }

        // ---- Revisa el nodo y rota si está desbalanceado ----
        private NodoAVL<T> Balancear(NodoAVL<T> nodo)
        {
            ActualizarAltura(nodo);
            int balance = FactorBalance(nodo);

            // Cargado a la IZQUIERDA (balance = 2)
            if (balance > 1)
            {
                if (FactorBalance(nodo.Izquierdo) < 0)                // zigzag izquierda-derecha
                    nodo.Izquierdo = RotarIzquierda(nodo.Izquierdo!);
                return RotarDerecha(nodo);
            }

            // Cargado a la DERECHA (balance = -2)
            if (balance < -1)
            {
                if (FactorBalance(nodo.Derecho) > 0)                  // zigzag derecha-izquierda
                    nodo.Derecho = RotarDerecha(nodo.Derecho!);
                return RotarIzquierda(nodo);
            }

            return nodo;   // ya estaba balanceado
        }

        // ---- Rotación a la derecha ----
        //        y               x
        //       / \             / \
        //      x   C    →      A   y
        //     / \                 / \
        //    A   B               B   C
        private NodoAVL<T> RotarDerecha(NodoAVL<T> y)
        {
            NodoAVL<T> x = y.Izquierdo!;
            NodoAVL<T>? b = x.Derecho;

            x.Derecho = y;
            y.Izquierdo = b;

            ActualizarAltura(y);
            ActualizarAltura(x);
            return x;   // x es la nueva raíz de esta parte
        }

        // ---- Rotación a la izquierda ----
        //      x                   y
        //     / \                 / \
        //    A   y      →        x   C
        //       / \             / \
        //      B   C           A   B
        private NodoAVL<T> RotarIzquierda(NodoAVL<T> x)
        {
            NodoAVL<T> y = x.Derecho!;
            NodoAVL<T>? b = y.Izquierdo;

            y.Izquierdo = x;
            x.Derecho = b;

            ActualizarAltura(x);
            ActualizarAltura(y);
            return y;   // y es la nueva raíz de esta parte
        }
    }
}