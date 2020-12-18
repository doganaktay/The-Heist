namespace Archi.BT
{
    public class AlwaysRandomSequence : Sequence
    {
        public AlwaysRandomSequence(string name, params Node[] childNodes) : base(name, childNodes)
        {
            ShuffleNodes();
        }

        protected override void OnReset()
        {
            base.OnReset();
            ShuffleNodes();
        }
    }
}