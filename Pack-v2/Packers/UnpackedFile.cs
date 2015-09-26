namespace Pack_v2.Packers
{
    public class UnpackedFile
    {
        private readonly string _name;
        private readonly byte[] _data;
        private readonly bool _secured;

        public UnpackedFile(string name, byte[] data, bool secured)
        {
            _name = name;
            _data = data;
            _secured = secured;
        }

        public string Name
        {
            get { return _name; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public bool Secured
        {
            get { return _secured; }
        }
    }
}