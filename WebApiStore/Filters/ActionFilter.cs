using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiStore.Filters
{
    public class ActionFilter : IActionFilter
    {
        private readonly ILogger<ActionFilter> logger;

        public ActionFilter(ILogger<ActionFilter> logger)
        {
            this.logger = logger;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            logger.LogInformation("La acción fue ya fue ejecutada");            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            logger.LogInformation("Ejecutando acción...");
        }
    }
}
