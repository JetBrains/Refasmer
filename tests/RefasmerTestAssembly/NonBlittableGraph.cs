namespace RefasmerTestAssembly;

public struct NonBlittableGraph
{
    private DubiouslyBlittableType x;
    private struct DubiouslyBlittableType
    {
        private NonBlittableType x;

        private struct NonBlittableType
        {
            private string x;
        }
    }
}
