using System.Collections.Generic;
using ConsoleDominioSmart.Modelos;

namespace ConsoleDominioSmart.Auxiliar
{
    public static class FilaDominioConsultar
    {
        public static Queue<DominioConsulta> Fila = new Queue<DominioConsulta>();

        public static void Enfileirar(DominioConsulta dominioConsulta)
        {
            Fila.Enqueue(dominioConsulta);
        }
    }
}
