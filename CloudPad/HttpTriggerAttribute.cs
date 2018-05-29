using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class HttpTriggerAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public HttpTriggerAttribute()
        {
            AuthLevel = AuthorizationLevel.Function;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>        
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(params string[] methods) : this()
        {
            Methods = methods;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel)
        {
            AuthLevel = authLevel;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel, params string[] methods)
        {
            AuthLevel = authLevel;
            Methods = methods;
        }

        /// <summary>
        /// Gets or sets the route template for the function. Can include
        /// route parameters using WebApi supported syntax. If not specified,
        /// will default to the function name.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets the http methods that are supported for the function.
        /// </summary>
        public string[] Methods { get; private set; }

        /// <summary>
        /// Gets the authorization level for the function.
        /// </summary>
        public AuthorizationLevel AuthLevel { get; private set; }
    }
}

