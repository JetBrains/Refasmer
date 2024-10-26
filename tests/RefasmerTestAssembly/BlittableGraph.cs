namespace RefasmerTestAssembly;

public struct BlittableGraph
{
    private BlittableType x;
    private struct BlittableType
    {
        private BlittableType2 x;

        private struct BlittableType2
        {
            private long x;
        }
    }
}
