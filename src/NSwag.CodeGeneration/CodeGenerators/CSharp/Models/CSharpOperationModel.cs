﻿using System.Collections.Generic;
using System.Linq;
using NJsonSchema;
using NSwag.CodeGeneration.CodeGenerators.Models;

namespace NSwag.CodeGeneration.CodeGenerators.CSharp.Models
{
    /// <summary>The CSharp operation model.</summary>
    /// <seealso cref="OperationModelBase" />
    public class CSharpOperationModel : OperationModelBase
    {
        private static readonly string[] ReservedKeywords =
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
            "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float",
            "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
            "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "virtual", "void", "volatile", "while"
        };

        private readonly SwaggerToCSharpGeneratorSettings _settings;
        private readonly SwaggerOperation _operation;
        private readonly SwaggerToCSharpGeneratorBase _generator;

        /// <summary>Initializes a new instance of the <see cref="CSharpOperationModel" /> class.</summary>
        /// <param name="operation">The operation.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="generator">The generator.</param>
        /// <param name="resolver">The resolver.</param>
        public CSharpOperationModel(
            SwaggerOperation operation, 
            ClientGeneratorBaseSettings settings, 
            SwaggerToCSharpGeneratorBase generator, 
            SwaggerToCSharpTypeResolver resolver)
            : base(resolver.ExceptionSchema, operation, resolver, generator, settings)
        {
            _settings = (SwaggerToCSharpGeneratorSettings)settings;
            _operation = operation;
            _generator = generator;

            // TODO: Duplicated code
            Parameters = _operation.ActualParameters.Select(parameter =>
                new ParameterModel(ResolveParameterType(parameter), _operation, parameter, parameter.Name,
                    GetParameterVariableName(parameter, _operation.Parameters), _settings.CodeGeneratorSettings,
                    _generator))
                .ToList();
        }

        /// <summary>Gets or sets the type of the result.</summary>
        public override string ResultType
        {
            get
            {
                if (UnwrappedResultType == "FileResponse")
                    return "System.Threading.Tasks.Task<FileResponse>";

                if ((_settings as SwaggerToCSharpClientGeneratorSettings)?.WrapSuccessResponses == true)
                    return UnwrappedResultType == "void"
                        ? "System.Threading.Tasks.Task<SwaggerResponse>"
                        : "System.Threading.Tasks.Task<SwaggerResponse<" + UnwrappedResultType + ">>";

                return UnwrappedResultType == "void"
                    ? "System.Threading.Tasks.Task"
                    : "System.Threading.Tasks.Task<" + UnwrappedResultType + ">";
            }
        }

        /// <summary>Gets or sets the type of the exception.</summary>
        public override string ExceptionType
        {
            get
            {
                if (_operation.Responses.Count(r => !HttpUtilities.IsSuccessStatusCode(r.Key)) != 1)
                    return "System.Exception";

                var response = _operation.Responses.Single(r => !HttpUtilities.IsSuccessStatusCode(r.Key)).Value;
                return _generator.GetTypeName(response.ActualResponseSchema, response.IsNullable(_settings.CodeGeneratorSettings.NullHandling), "Exception");
            }
        }

        /// <summary>Gets the name of the parameter variable.</summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="allParameters">All parameters.</param>
        /// <returns>The parameter variable name.</returns>
        protected override string GetParameterVariableName(SwaggerParameter parameter, IEnumerable<SwaggerParameter> allParameters)
        {
            var name = base.GetParameterVariableName(parameter, allParameters);
            return ReservedKeywords.Contains(name) ? "@" + name : name;
        }

        /// <summary>Resolves the type of the parameter.</summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The parameter type name.</returns>
        protected override string ResolveParameterType(SwaggerParameter parameter)
        {
            var schema = parameter.ActualSchema;
            if (schema.Type == JsonObjectType.File)
            {
                if (parameter.CollectionFormat == SwaggerParameterCollectionFormat.Multi && !schema.Type.HasFlag(JsonObjectType.Array))
                    return "System.Collections.Generic.IEnumerable<FileParameter>";

                return "FileParameter";
            }

            return base.ResolveParameterType(parameter)
                .Replace(_settings.CSharpGeneratorSettings.ArrayType + "<", "System.Collections.Generic.IEnumerable<")
                .Replace(_settings.CSharpGeneratorSettings.DictionaryType + "<", "System.Collections.Generic.IDictionary<");
        }
    }
}
