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
    public interface IAPI : IControllerPrefix { }

    [Name("v1")]
    public interface IApiV1 : IAPI { }

    [Name("entry")]
    public class ApiExampleController : Controller, IApiV1
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

        [HttpGet, Route("{id}/detail")]
        public IResult Detail(int id)
        {
            return Status(NoContent, "I have no content :(");
        }
    }

}
