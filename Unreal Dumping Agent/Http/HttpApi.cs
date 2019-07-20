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

    public class ControllerItems
    {
        public const string ENTRY_DEFAULT_INDEX = "GET /api/v1/entry";
        public const string ENTRY_UPDATE = "POST /api/v1/entry/update";
        public const string ENTRY_INDEX_ID = "GET /api/v1/entry/id";
        public const string ENTRY_DETAIL_INDEX = "GET /api/v1/entry/detail";
        public const string ENTRY_DETAIL_CROSS = "GET /api/v1/entry/cross/id";
        public const string ENTRY_DETAIL_ID = "GET /api/v1/entry/detail/id";

        [Name("entry")]
        public class ApiExampleController : Controller, IApiV1
        {

            [HttpGet]
            public IResult Index(IHttpContext context)
            {
                return Status(HttpStatusCode.OK, ENTRY_DEFAULT_INDEX);
            }

            [HttpGet]
            [Route("{id}/vss-{*blurp}")]
            [Route("{id}")]
            public IResult Index(IHttpContext context, [Parameter(Source = ParameterSource.Url)]int id, string blurp = "my-blurp")
            {
                return Status(HttpStatusCode.OK, ENTRY_INDEX_ID);
            }

            [Route("detail/{id}")]
            public IResult Detail(IHttpContext context, int id)
            {
                return Status(HttpStatusCode.OK, ENTRY_DETAIL_ID);
            }

            [Route("{id}/detail")]
            public IResult Cross(IHttpContext context, int id)
            {
                return Status(HttpStatusCode.OK, ENTRY_DETAIL_CROSS);
            }

            public IResult Detail(IHttpContext context)
            {
                return Status(HttpStatusCode.OK, ENTRY_DETAIL_INDEX);
            }

        }
    }

}
