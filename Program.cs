using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IServAPI
{
    internal class Program
    {
         
        
        
        static async Task Main(string[] args)
        {
            HttpClient client = new HttpClient();
            string server = "aeg-buchholz.com";
            var iserv = new iserv();
            await iserv.UserAuth(client, server);
            await iserv.getMail(client, server);
            
        }
    }
}

