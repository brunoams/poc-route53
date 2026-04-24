using System.Collections.Concurrent;
using ConsoleDominioSmart.Modelos;

namespace ConsoleDominioSmart.Auxiliar
{
    public class FilaDominioConsultar
    {
        private readonly ConcurrentQueue<DominioConsulta> _fila = new();

        public void Enfileirar(DominioConsulta dominioConsulta) => _fila.Enqueue(dominioConsulta);

        public bool TryDequeue(out DominioConsulta dominioConsulta) => _fila.TryDequeue(out dominioConsulta!);
    }
}
