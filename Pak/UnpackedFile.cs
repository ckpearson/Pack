namespace Pack
{
    public class UnpackedFile
    {
        private readonly string _name;
        private readonly byte[] _data;

        public UnpackedFile(string name, byte[] data)
        {
            _name = name;
            _data = data;
        }

        public string Name
        {
            get { return _name; }
        }

        public byte[] Data
        {
            get { return _data; }
        }
    }
}