using Microsoft.Extensions.Configuration.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace CsRemoveComment
{
    class Program
    {
        //待处理列表
        private static List<string> sb = new List<string>();
        //排除获取文件夹
        private readonly static List<String> _exclusion = new List<String>
                                                   {
                                                       "bin",
                                                       "obj"
                                                   };
        static void Main(string[] args)
        {
            var cmdLineConfig = new CommandLineConfigurationProvider(args);
            cmdLineConfig.Load();

            cmdLineConfig.TryGet("From", out string FromFile);
            cmdLineConfig.TryGet("To", out string ToFile);
            if (string.IsNullOrWhiteSpace(FromFile) || string.IsNullOrWhiteSpace(ToFile))
            {        
                var path = Environment.CurrentDirectory;
                var input = File.ReadAllText($@"{path}\text\TestClass.csts");
                RemoveAllCommentStr(input, out string noComments2, out List<string> list2);

                foreach (var comment in list2)
                    Console.WriteLine(comment);
                Console.WriteLine("--------------");
                Console.WriteLine(noComments2);
                Console.WriteLine("没有设置正确参数From(来源文件夹)和To(目标文件夹)");
                Console.ReadLine();

                return;
            }


            Console.WriteLine("等待扫描来源文件夹");
            //获得来源文件
            GetFiles(new DirectoryInfo(FromFile), "*.*");
            foreach (var item in sb)
            {
                var newFullFile = item.Replace(FromFile, ToFile);
                var newPath = Path.GetDirectoryName(newFullFile);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                //处理文件
                if (!File.Exists(newFullFile) && Path.GetExtension(newFullFile).Contains(".cs"))
                {
                    var file = File.ReadAllText(item);
                    RemoveAllCommentStr(file, out string noComments, out List<string> list);
                    File.WriteAllText(newFullFile, noComments);
                    Console.WriteLine($"处理文件:{newFullFile}");
                }
                else if (!File.Exists(newFullFile) && !Path.GetExtension(newFullFile).Contains(".cs"))
                {
                    File.Copy(item, newFullFile);
                    Console.WriteLine($"复制:{newFullFile}");
                }

            }
            Console.WriteLine("完成");
            Console.ReadLine();            
        }





        public static void GetFiles(DirectoryInfo directory, string pattern)
        {
            if (directory.Exists || pattern.Trim() != string.Empty)
            {
                foreach (FileInfo info in directory.GetFiles(pattern))
                {
                    sb.Add(info.FullName.ToString());
                }
                foreach (DirectoryInfo info in directory.GetDirectories().Where(d => !FoundInArray(_exclusion, d.FullName)))
                {
                    GetFiles(info, pattern);
                }
            }
        }
        public static bool FoundInArray(List<string> arr, string target)
        {
            return arr.Any(p => new DirectoryInfo(target).Name == p);
        }

        /// <summary>
        ///删除注释
        ///参考来自https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689
        /// </summary>
        /// <param name="input"></param>
        /// <param name="outlist"></param>
        /// <param name="noComments"></param>
        private static void RemoveAllCommentStr(string input, out string noComments, out List<string> outlist)
        {
            var list = new List<string>();
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)(\r?\n|$)";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            noComments = Regex.Replace(input,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                    {
                        // 将注释内容放入列表中
                        list.Add(me.Groups[1].Value + me.Groups[2].Value);
                        // 将注释替换为空, 即删除它们
                        return me.Value.StartsWith("//") ? me.Groups[3].Value : "";
                    }
                    // 保留字面字符串
                    return me.Value;
                },
                RegexOptions.Singleline);
            outlist = list;
        }
    }

}
