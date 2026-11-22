namespace ConviTEC.Core.Estructuras;

public static class Ordenamiento
{
    public static T[] PorMezcla<T>(Lista<T> lista, Comparison<T> comparar)
    {
        var datos = lista.ToArray();
        var auxiliar = new T[datos.Length];
        Mezclar(0, datos.Length);
        return datos;

        void Mezclar(int inicio, int fin)
        {
            if (fin - inicio < 2) return;
            int mitad = inicio + (fin - inicio) / 2;
            Mezclar(inicio, mitad);
            Mezclar(mitad, fin);
            int izq = inicio, der = mitad;
            for (int i = inicio; i < fin; i++)
            {
                bool tomarIzquierda = der == fin || (izq < mitad && comparar(datos[izq], datos[der]) <= 0);
                auxiliar[i] = tomarIzquierda ? datos[izq++] : datos[der++];
            }
            Array.Copy(auxiliar, inicio, datos, inicio, fin - inicio);
        }
    }
}
