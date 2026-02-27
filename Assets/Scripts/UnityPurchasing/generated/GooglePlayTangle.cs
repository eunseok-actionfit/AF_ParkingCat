// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("wzVSUdrfqjPNUZHHMlgjeVyTr7k6ube4iDq5sro6ubm4evejtbG2IX76AwqpBKrMScev/1wZ19EVIaLIa/D4tDhSpUVaZejCuRX3Yo7Ch2KyBGFsAvmMUKU9Ib7m60N65erUIFUWsoyxcXm67HrOkVS8vcZZ6JisESUogxoX1A83Tlc3/sZBJDr/FKJMCYuQefq3pvmCsSN7zSQQr4QJBtUq5rAm0fKsNR342SWdpaTOdZeCJZhjqxqlJaBCphhBQi2BhB1aZB5A6h/06qpO+GmXyMj3143MCh8FSzjOX5HItUbmwminZ37iLfxMLT2AiDq5moi1vrGSPvA+T7W5ubm9uLvTTwIRJeIyG21V0Kf9MvC3IApMnTokOPvtQnd6Gbq7ubi5");
        private static int[] order = new int[] { 5,1,12,7,13,7,12,12,9,9,11,11,12,13,14 };
        private static int key = 184;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
