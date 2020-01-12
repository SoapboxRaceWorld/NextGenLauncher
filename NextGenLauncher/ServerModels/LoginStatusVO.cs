using System.Xml.Serialization;

namespace NextGenLauncher.ServerModels
{
    public class LoginStatusVO
    {
        [XmlElement("LoginToken", IsNullable = false)]
        public string LoginToken;

        [XmlElement("UserId", IsNullable = false)]
        public int UserID;

        [XmlElement("Description", IsNullable = true)]
        public string Description;
    }
}