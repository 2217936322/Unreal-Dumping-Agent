using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ceen;
using Ceen.Mvc;

namespace Unreal_Dumping_Agent.Http
{
    [Name("api")]
    internal interface IApi : IControllerPrefix { }

    [Name("v1")]
    internal interface IApiV1 : IApi { }

    [Name("entry")]
    public class ApiUdaController : Controller, IApiV1
    {
        [HttpGet]
        public IResult Index(IHttpContext context)
        {
            return OK;
        }

        public IResult Index(int id)
        {
            return Html("<body>Hello!</body>");
        }

        [Route("{id}/detail")]
        public IResult Detail(int id)
        {
            return Status(NoContent, "I have no content :(");
        }
    }
}
