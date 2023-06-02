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
    public static class CaaSHost
    {
        delegate byte[] EndpointMethod(string args);

        public static readonly string Root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static Dictionary<string, Tuple<EndpointMethod, CaaSEndpoint>> Endpoints = new Dictionary<string, Tuple<EndpointMethod, CaaSEndpoint>>();

        static void Main(string[] args)
        {
            // load modules
            string[] mods = Directory.GetFiles(Path.Combine(Root, "modules"), "*.dll");
            if (mods.Length == 0)
            {
                throw new ArgumentOutOfRangeException("apologies, but at least one module must be present in the modules directory. ^^;");
            }
            foreach (string mod in mods)
            {
                Assembly asm = Assembly.LoadFrom(mod);
                Console.WriteLine("load: " + asm.GetName());
                Type[] types = asm.GetModules().First().GetTypes();
                foreach (Type t in types)
                {
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (MethodInfo method in methods)
                    {
                        CaaSEndpoint attr = (CaaSEndpoint)method.GetCustomAttributes(typeof(CaaSEndpoint), false).FirstOrDefault();
                        if (attr != null)
                        {
                            string mount = (attr).Endpoint;
                            Endpoints[mount] = new Tuple<EndpointMethod, CaaSEndpoint>(
                                (EndpointMethod)Delegate.CreateDelegate(typeof(EndpointMethod), method),
                                attr
                            );
                        }
                    }
                }
            }

            // start http server
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
                    if (Endpoints.ContainsKey(req.Url.AbsolutePath))
                    {
                        var endpoint = Endpoints[req.Url.AbsolutePath];
                        byte[] buffer = endpoint.Item1(input);
                        res.ContentType = endpoint.Item2.ContentType;
                        res.ContentLength64 = buffer.Length;
                        res.OutputStream.Write(buffer, 0, buffer.Length);
                        res.OutputStream.Close();
                    }
                    else
                    {
                        res.StatusCode = 404;
                        res.Close();
                        Console.WriteLine(404);
                        continue;
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
