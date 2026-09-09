using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KoiCafe.Filters
{
    public class AdminAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("VaiTro");
            
            if (string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            if (role != "Admin")
            {
                context.Result = new ContentResult
                {
                    Content = "<h2 style='color:red;text-align:center;margin-top:50px;'>🛑 BẠN KHÔNG CÓ QUYỀN VÀO TRANG NÀY!</h2>",
                    ContentType = "text/html"
                };
            }
            
            base.OnActionExecuting(context);
        }
    }
}