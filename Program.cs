using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF_HW_DecodeBenchmark
{
    internal class Program
    {

        // THE GOAL: 
        // Find out if hw decoding is supported for a given format
        // 
        // HOW:
        // Decode in software
        // Decode in hardware (-hwaccel auto)
        // find difference

        // EXECUTION: 

        // for each mov/mp4 file in the current dir do
        // ffmpeg -i INPUT_FILE -benchmark -f null - > INPUT_FILE-SW.txt
        // ffmpeg -hwaccel auto -i INPUT_FILE -benchmark -f null - > INPUT_FILE-HW.txt
        // ffmpeg -hwaccel cuda -i INPUT_FILE -benchmark -f null - > INPUT_FILE-HW-Cuda.txt
        // ffmpeg -c:v h264_cuvid -i INPUT_FILE -benchmark -f null - > INPUT_FILE-HW-cuvid.txt
        // ffmpeg -hwaccel nvdec -i INPUT_FILE -benchmark -f null - > INPUT_FILE-HW-nvdec.txt

        // then 
        // for each txt file in the current dir do
        // if line.contains("bench: utime=")
        // then 
        // write INPUT_FILENAME+ bechnmark_time to a new file

        // Secondary: make a result file with all the HWaccel names and their average time taken, so compare them over many files.


        static string compareBatFilename = "0-compare.bat";
        static List<string> Filenames_and_speed = new List<string>(); // the list where every piece of information is stored.

        static string[] HWaccelArr = { "-hwaccel auto", "-hwaccel cuda", "-hwaccel d3d11va", "-hwaccel d3d12va", "-hwaccel dxva2","", "-hwaccel nvdec", "-hwaccel opencl", "-hwaccel vulkan"  };
        static string[] HWaccelShortArr = { "auto", "cuda", "d3d11va", "d3d12va", "dxva2", "NONE", "nvdec", "opencl", "vulkan" };


        static void filegen()
        {
            List<string> ffmpegBatList = new List<string>();

            foreach (var COMPAREFileFromDir in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.mov"))
                {
                    ffmpegBatList.AddRange(CreateBatContentList(COMPAREFileFromDir));

                }
                foreach (var COMPAREFileFromDir in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.mp4"))
                {
                    ffmpegBatList.AddRange(CreateBatContentList(COMPAREFileFromDir));

                }
                foreach (var COMPAREFileFromDir in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.mkv"))
                {
                    ffmpegBatList.AddRange(CreateBatContentList(COMPAREFileFromDir));

                }
            
            

            File.WriteAllLines(compareBatFilename, ffmpegBatList);
        }

        static List<string> CreateBatContentList(string InputFile)
        {
            List<string> batList = new List<string>();


            string InputFileWithExtension = InputFile.Substring(InputFile.LastIndexOf("\\") + 1);
            string InputCorrect = InputFileWithExtension.Substring(0, InputFileWithExtension.Length - 4);

            

            for (int HWaccelInt = 0; HWaccelInt < HWaccelArr.Length; HWaccelInt++)
            {
                string ffStr = "ffmpeg " + HWaccelArr[HWaccelInt] + " -i " + InputFileWithExtension+ " -benchmark -f null - > ";
                string txtStr = InputFileWithExtension + "_"+HWaccelShortArr[HWaccelInt] + ".txt 2>&1";

                ffStr = ffStr + txtStr;

                batList.Add(ffStr);

            }
            return batList;

        }

        static void WriteResult()
        {
            int numberOfTxt = 0;
            string resultStr = "";

            foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.txt"))
            {
                string FilenameWithExtention = file.Substring(file.LastIndexOf("\\") + 1);
                string FileNameNoExt = FilenameWithExtention.Substring(0, FilenameWithExtention.Length - 4);
                var lines = File.ReadLines(file);
                
                foreach (var line in lines)
                {
                    if (line.Contains("rtime="))
                    {
                        resultStr = ReadResultLine(line, FileNameNoExt);
                        if (resultStr.Length > 0)
                        {
                            Filenames_and_speed.Add(resultStr); 
                        }


                    }

                }
            }

            WriteResultFile(numberOfTxt);

        }
        static void WriteResultFile(int numberOfTxt)
        {
            try
            {
                StreamWriter sw = new StreamWriter("1Speed.txt");
                
                foreach (var line in Filenames_and_speed)
                {
                    sw.WriteLine(line);
                }

                sw.Close();
            }
            catch (Exception)
            {

            }

        }

        static string ReadResultLine(string line, string filename) // read result from ffmpeg output text file
        {
            string resultStr = "";


            if (line.Contains("rtime=") )
            {
                // source line: 
                //bench: utime=0.312s stime=0.188s rtime=5.435s
                
                string rtimeStr = line.Substring(line.LastIndexOf("rtime")+6); // substring from the actual time to the end of the line

                string HWaccType = filename.Substring(filename.LastIndexOf("_") + 1); //same as the list above
                
                string filenameNoExt = filename.Substring(0,filename.LastIndexOf("_")); 

                //Console.WriteLine(filenameNoExt+" "+rtimeStr);
                resultStr = filenameNoExt + ";"+HWaccType+ ";" + rtimeStr;  // made this way to give a spreadsheet a possibility to compare HWaccel type against each other,
                                                                            // as well as compare CRF0 vs CRF 23 vs CRF 69

            }

            return resultStr;
        }


        static void Main(string[] args)
        {

            filegen();
            
            Console.WriteLine("Want to execute the .bat file? Y/N with enter");
            if (Console.ReadLine() == "y")
            {
                Process p = new Process();
                p.StartInfo.FileName = compareBatFilename;
                p.Start();
                p.WaitForExit();

                // wait for the processing to stop before moving forward

            }
            else
            {

            }


            WriteResult();




        }
    }
}
