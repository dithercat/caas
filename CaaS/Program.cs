// kasumi: win32 microservice

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Reflection;

namespace CaaS
{
    class Program
    {
        static void Main(string[] args)
        {
            string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("apologies, but Windows XP SP2+ is required for this application. ^^;");
            }

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:80/");
            listener.Start();

            Console.WriteLine("listening! ^^");
            while (true)
            {
                HttpListenerContext ctx = listener.GetContext();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;
                try
                {
                    Console.WriteLine(req.HttpMethod + " " + req.Url.AbsolutePath);
                    if (req.HttpMethod != "POST")
                    {
                        res.StatusCode = 405;
                        res.Close();
                        Console.WriteLine(405);
                        continue;
                    }

                    // read post body
                    if (!req.HasEntityBody || req.ContentType != "text/plain")
                    {
                        res.StatusCode = 400;
                        res.Close();
                        Console.WriteLine(400);
                        continue;
                    }
                    StreamReader reader = new StreamReader(req.InputStream);
                    string input = reader.ReadToEnd();
                    Console.WriteLine("text: " + input);

                    // routes
                    switch (req.Url.AbsolutePath)
                    {
                        // clippy renderer
                        case "/clippy":
                            {
                                byte[] buffer = Clippy.DrawClippy(input);
                                res.ContentType = "image/png";
                                res.ContentLength64 = buffer.Length;
                                res.OutputStream.Write(buffer, 0, buffer.Length);
                                res.OutputStream.Close();
                                break;
                            }
                        // generate audio with say.exe
                        case "/dectalk":
                            {
                                string tmpfile = Path.GetTempPath() + "\\dectalk.wav";
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    FileName = root + "\\bin\\say.exe",
                                    Arguments = "-pre \"[:phone on]\" -w \"" + tmpfile + "\"",
                                    WorkingDirectory = root + "\\bin",
                                    RedirectStandardInput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                Process proc = Process.Start(psi);
                                proc.StandardInput.Write(input);
                                proc.StandardInput.Close();
                                proc.WaitForExit();
                                byte[] buffer = File.ReadAllBytes(tmpfile);
                                res.ContentType = "audio/wav";
                                res.ContentLength64 = buffer.Length;
                                res.OutputStream.Write(buffer, 0, buffer.Length);
                                res.OutputStream.Close();
                                break;
                            }
                        // 404
                        default:
                            {
                                res.StatusCode = 404;
                                res.Close();
                                Console.WriteLine(404);
                                continue;
                            }
                    }
                }
                catch (Exception ex)
                {
                    res.StatusCode = 500;
                    res.Close();
                    Console.Write(500);
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
