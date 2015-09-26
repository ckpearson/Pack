using System;
using System.Xml.Linq;

namespace Pack_v2.Security
{
    public static class PackKeyEncoder
    {
        public static XElement ToXml(PackKey key)
        {
            return new XElement("PackKey",
                new XElement("pub", Convert.ToBase64String(key.PublicKey)),
                new XElement("iv", Convert.ToBase64String(key.InitialisationVector)),
                new XElement("salt", Convert.ToBase64String(key.Salt)),
                new XElement("priv", Convert.ToBase64String(key.EncryptedPrivateKey)),
                new XElement("challenge", Convert.ToBase64String(Encryption.RsaEncrypt(key.PublicKey, key.Salt))));
        }

        public static PackKey FromXml(XElement xml)
        {
            return new PackKey(
                Convert.FromBase64String(xml.Element("pub").Value),
                Convert.FromBase64String(xml.Element("priv").Value),
                Convert.FromBase64String(xml.Element("iv").Value),
                Convert.FromBase64String(xml.Element("salt").Value),
                Convert.FromBase64String(xml.Element("challenge").Value)
                );
        }
    }
}