// using Microsoft.AspNetCore.Hosting;
// using minimal_api;

// namespace minimal_api
// {
//     public class Program
//     {
//         public static void Main(string[] args)
//         {
//             CreateHostBuilder(args).Build().Run();
//         }

//         public static IHostBuilder CreateHostBuilder(string[] args)

//         {
//             return Host.CreateDefaultBuilder(args)
//                 .ConfigureWebHostDefaults(webBuilder =>
//                 {
//                     webBuilder.UseStartup<IStartup>();
//                 });
//         }
//     }

// }




// using Microsoft.AspNetCore.Hosting;
// using Microsoft.Extensions.Hosting;

// namespace minimal_api
// {
//     public class Program
//     {
//         public static void Main(string[] args)
//         {
//             CreateHostBuilder(args).Build().Run();
//         }

//         public static IHostBuilder CreateHostBuilder(string[] args) =>
//             Host.CreateDefaultBuilder(args)
//                 .ConfigureWebHostDefaults(webBuilder =>
//                 {
//                     webBuilder.UseStartup<Startup>();
//                 });
//     }
// }


 IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
  
}
CreateHostBuilder(args).Build().Run();