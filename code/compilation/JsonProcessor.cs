using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DCAF.Squawks
{
    public class JsonProcessor
    {
        public async Task<Stream> LoadAndCleanJsonAsync(FileSystemInfo file)
        {
            const string SingleLineCommentQualifier = "//";
            const string MultiLineCommentQualifierPrefix = "/*";
            const string MultiLineCommentQualifierSuffix = "*/";

            try
            {
                var sb = new StringBuilder();
                var ca = (await File.ReadAllTextAsync(file.FullName)).ToCharArray();
                for (var i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    if (c == '\"')
                    {
                        eatUntil("\"", true);
                        continue;
                    }

                    if (ca.IsToken(i, SingleLineCommentQualifier))
                    {
                        skipUntil(Environment.NewLine);
                        continue;
                    }

                    if (ca.IsToken(i, MultiLineCommentQualifierPrefix))
                    {
                        skipUntil(MultiLineCommentQualifierSuffix);
                        i += MultiLineCommentQualifierSuffix.Length;
                        continue;
                    }

                    sb.Append(c);

                    void eatUntil(string terminator, bool include)
                    {
                        if (include)
                        {
                            sb.Append(ca[i++]);
                        }

                        for (; i < ca.Length && !ca.IsToken(i, terminator); i++)
                        {
                            sb.Append(ca[i]);
                        }

                        if (include)
                        {
                            sb.Append(ca[i]);
                        }
                    }

                    void skipUntil(string terminator)
                    {
                        for (; i < ca.Length && !ca.IsToken(i, terminator); i++)
                        {
                        }
                    }

                    // bool isToken(string pattern) obsolete
                    // {
                    //     var pa = pattern.ToCharArray();
                    //     for (var j = 0; j < pa.Length && j < ca.Length; j++)
                    //     {
                    //         if (ca[i + j] != pa[j])
                    //             return false;
                    //     }
                    //
                    //     return true;
                    // }
                }

                var byteArray = Encoding.ASCII.GetBytes(sb.ToString());
                return new MemoryStream(byteArray);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

        }
    }
}