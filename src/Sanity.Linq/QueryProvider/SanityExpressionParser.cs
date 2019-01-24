// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.If not, see<https://www.gnu.org/licenses/>.


using Newtonsoft.Json;
using Sanity.Linq.CommonTypes;
using Sanity.Linq.Extensions;
using Sanity.Linq.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Sanity.Linq
{
    internal class SanityExpressionParser : ExpressionVisitor
    {

        public SanityExpressionParser(Expression expression, Type docType, Type resultType = null)
        {
            Expression = expression;
            DocType = docType;
            ResultType = TypeSystem.GetElementType(resultType);
        }


        private SanityQueryBuilder QueryBuilder { get; set; } = new SanityQueryBuilder();
        public Expression Expression { get; }
        public Type DocType { get; }
        public Type ResultType { get; }

        public string BuildQuery(bool includeProjections = true)
        {            
            //Initialize query builder
            QueryBuilder = new SanityQueryBuilder();

            // Add contraint for root type
            QueryBuilder.DocType = DocType;
            QueryBuilder.ResultType = ResultType ?? DocType;

            // Parse Query
            if (Expression is MethodCallExpression || Expression is LambdaExpression)
            {
                // Traverse expression to build query
                Visit(Expression);
            }



            // Build query
            return QueryBuilder.Build(includeProjections);
            
        }


        public override Expression Visit(Expression expression)
        {
            if (expression == null) return expression;
            if (expression is LambdaExpression l)
            {
                //Simplify lambda
                expression = (LambdaExpression)Evaluator.PartialEval(expression);
                if (((LambdaExpression)expression).Body is MethodCallExpression method)
                {
                    QueryBuilder.Constraints.Add(TransformMethodCallExpression(method));
                }
            }
            if (expression is BinaryExpression b)
            {
                QueryBuilder.Constraints.Add(TransformBinaryExpression(b));
                return b;
            }
            if (expression is UnaryExpression u)
            {
                QueryBuilder.Constraints.Add(TransformUnaryExpression(u));
                return u;
            }
            if (expression is MethodCallExpression m)
            {
                TransformMethodCallExpression(m);
                return expression;

            }
            return base.Visit(expression);
        }


        protected string TransformBinaryExpression(BinaryExpression b)
        {
            string left, op, right;
            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                    {
                        op = "==";
                        break;
                    }
                case ExpressionType.AndAlso:
                    {
                        op = "&&";
                        break;
                    }
                case ExpressionType.OrElse:
                    {
                        op = "||";
                        break;
                    }
                case ExpressionType.LessThan:
                    {
                        op = "<";
                        break;
                    }
                case ExpressionType.GreaterThan:
                    {
                        op = ">";
                        break;
                    }
                case ExpressionType.LessThanOrEqual:
                    {
                        op = "<=";
                        break;
                    }
                case ExpressionType.GreaterThanOrEqual:
                    {
                        op = ">=";
                        break;
                    }
                case ExpressionType.NotEqual:
                    {
                        op = "!=";
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException($"Operator '{b.NodeType}' is not supported.");
                    }
            }

            left = TransformOperand(b.Left);
            right = TransformOperand(b.Right);

            // Handle comparison to null
            if (right == "null" && op == "==")
            {
                return $"(!(defined({left})) || {left} {op} {right})";
            }
            else if (right == "null" && op == "!=")
            {
                return $"(defined({left}) && {left} {op} {right})";
            }
            else
            {
                return $"({left} {op} {right})";
            }
        }

        protected string TransformMethodCallExpression(MethodCallExpression e)
        {
            switch (e.Method.Name)
            {
                case "StartsWith":
                    {
                        var memberName = TransformOperand(e.Object);
                        var value = "";
                        if (e.Arguments[0] is ConstantExpression c && c.Type == typeof(string))
                        {
                            value = c.Value?.ToString() ?? "";
                        }
                        else
                        {
                            throw new Exception("StartsWith is only supported for constant expressions");
                        }

                        return $"{memberName} match \"{value}*\"";
                    }
                case "GetValue`1":
                case "GetValue":
                    {
                        if (e.Arguments.Count > 0)
                        {
                            var fieldName = "";
                            var simplifiedExpression = Evaluator.PartialEval(e.Arguments[1]);
                            if (simplifiedExpression is ConstantExpression c && c.Type == typeof(string))
                            {
                                fieldName = c.Value?.ToString() ?? "";
                                return $"{fieldName}";
                            }
                        }
                        throw new Exception("Could not evaluate GetValue method");
                    }
                case "Where":
                    {
                        //Arg 0: Source
                        var elementType = TypeSystem.GetElementType(e.Arguments[0].Type);
                        if (elementType != DocType)
                        {
                            throw new Exception("Where expressions are only supported on the root type.");
                        }
                        Visit(e.Arguments[0]);

                        //Arg 1: Query / lambda
                        if (e.Arguments[1] is UnaryExpression u && u.Operand is LambdaExpression l)
                        {
                            var constraint = TransformOperand(l.Body);
                            QueryBuilder.Constraints.Add(constraint);
                            return constraint;
                        }
                        throw new Exception("Syntax of Select expression not supported.");
                    }
                case "Select":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);

                        //Arg 1: Select expression
                        if (e.Arguments[1] is UnaryExpression u && u.Operand is LambdaExpression l)
                        {
                            if (l.Body is MemberExpression m && (m.Type.IsPrimitive || m.Type == typeof(string)))
                            {
                                throw new Exception($"Selecting '{m.Member.Name}' as a scalor value is not supported due to serialization limitations. Instead, create an anonymous object containing the '{m.Member.Name}' field. e.g. o => new {{ o.{m.Member.Name} }}.");
                            }
                            var projection = TransformOperand(l.Body);
                            QueryBuilder.Projection = projection;
                            return projection;
                        }
                        throw new Exception("Syntax of Select expression not supported.");
                    }
                case "Include":
                    {
                        //Arg 0: Source

                        // Arg 1: Field to join
                        if (e.Arguments[1] is UnaryExpression u && u.Operand is LambdaExpression l)
                        {
                            if (l.Body is MemberExpression m && m.Member.MemberType == MemberTypes.Property)
                            {
                                var fieldName = TransformOperand(l.Body);
                                var propertyType = l.Body.Type;
                                var projection = GetJoinProjection(fieldName, propertyType);
                                QueryBuilder.Includes[fieldName] = projection;
                                return projection;
                            }
                        }
                        throw new Exception("Joins can only be applied to properties.");

                    }
                case "IsNullOrEmpty":
                    {
                        var field = TransformOperand(e.Arguments[0]);
                        return $"{field} == null || {field} == \"\" || !(defined({field}))";
                    }
                case "_id":
                case "SanityId":
                    {
                        return "_id";
                    }
                case "_createdAt":
                case "SanityCreatedAt":
                    {
                        return "_createdAt";
                    }
                case "_updatedAt":
                case "SanityUpdatedAt":
                    {
                        return "_updatedAt";
                    }
                case "_rev":
                case "SanityRevision":
                    {
                        return "_rev";
                    }
                case "_type":
                case "SanityType":
                    {
                        return "_type";
                    }
                case "IsDefined":
                    {
                        var field = TransformOperand(e.Arguments[0]);
                        return $"defined({field})";
                    }
                case "IsDraft":
                    {
                        return $"_id in path(\"drafts.**\")";
                    }
                case "Cast":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);
                        return "";
                    }
                case "OrderBy":
                case "ThenBy":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);

                        // Args[1] Order expression
                        if (e.Arguments[1] is UnaryExpression u && u.Operand is LambdaExpression l)
                        {
                            var declaringType = l.Parameters[0].Type;
                            if (declaringType != DocType)
                            {
                                throw new Exception($"Ordering is only supported on root document type {DocType.Name ?? ""}");
                            }
                            var ordering = TransformOperand(l.Body) + " asc";
                            QueryBuilder.Orderings.Add(ordering);
                            return ordering;
                        }
                        throw new Exception("Order by expression not supported.");
                    }
                case "OrderByDescending":
                case "ThenByDescending":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);

                        // Args[1] Order expression
                        if (e.Arguments[1] is UnaryExpression u && u.Operand is LambdaExpression l)
                        {
                            var declaringType = l.Parameters[0].Type;
                            if (declaringType != DocType)
                            {
                                throw new Exception($"Ordering is only supported on root document type {DocType.Name ?? ""}");
                            }
                            var ordering = TransformOperand(l.Body) + " desc";
                            QueryBuilder.Orderings.Add(ordering);
                            return ordering;
                        }
                        throw new Exception("Order by descending expression not supported.");
                    }
                case "Count":
                case "LongCount":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);
                        string function = "count";
                        QueryBuilder.AggregateFunction = function;
                        return function;
                    }
                case "Take":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);

                        //Arg 1: take
                        if (e.Arguments[1] is ConstantExpression c)
                        {
                            QueryBuilder.Take = (int)c.Value;
                            return QueryBuilder.Take.ToString();
                        }
                        throw new Exception("Format for Take expression not supported.");
                    }
                case "Skip":
                    {
                        //Arg 0: Source
                        Visit(e.Arguments[0]);

                        //Arg 1: take
                        if (e.Arguments[1] is ConstantExpression c)
                        {
                            QueryBuilder.Skip = (int)c.Value;
                            return QueryBuilder.Skip.ToString();
                        }
                        throw new Exception("Format for Skip expression not supported.");
                    }
                default:
                    {
                        throw new Exception($"Method call {e.Method.Name} not supported.");
                    }
            }
        }

        protected string TransformUnaryExpression(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Not)
            {
                return "!(" + TransformOperand(u.Operand) + ")";
            }
            if (u.NodeType == ExpressionType.Convert)
            {
                return TransformOperand(u.Operand);
            }
            throw new Exception($"Unary expression of type {u.GetType()} and nodeType {u.NodeType} not supported. ");
        }


        protected string TransformOperand(Expression e)
        {
            // Attempt to simplyfy
            e = Evaluator.PartialEval(e);

            // Member access
            if (e is MemberExpression m)
            {
                var memberPath = new List<string>();
                var member = m.Member;

                if (member.Name == "Value" && member.DeclaringType.IsGenericType && member.DeclaringType.GetGenericTypeDefinition() == typeof(SanityReference<>))
                {
                    memberPath.Add("->");
                }
                else
                {
                    var jsonProperty = member.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute;
                    if (jsonProperty != null)
                    {
                        memberPath.Add(jsonProperty.PropertyName);
                    }
                    else
                    {
                        memberPath.Add(member.Name.ToCamelCase());
                    }
                }
                if (m.Expression is MemberExpression)
                {
                    memberPath.Add(TransformOperand(m.Expression));
                }
                
                return memberPath.Aggregate((a1, a2) => a1 != "->" && a2 != "->" ? $"{a2}.{a1}" : $"{a2}{a1}").Replace(".->","->").Replace("->.","->");
            }

            if (e is NewExpression nw)
            {
                // New expression with support for nested news
                var args = nw.Arguments.Select(arg => arg is NewExpression ? "{" +  TransformOperand(arg) + "}" : TransformOperand(arg)).ToArray();
                var props = nw.Members.Select(prop => prop.Name.ToCamelCase()).ToArray();
                if (args.Length == props.Length)
                {
                    var projection = new List<string>();
                    for (var i = 0; i < args.Length; i++)
                    {
                        if (args[i].Equals(props[i]))
                        {
                            projection.Add(args[i]);
                        }
                        else
                        {
                            projection.Add($"\"{props[i]}\": {args[i]}");
                        }
                    }
                    return projection.Aggregate((pc, pn) => $"{pc}, {pn}");
                }
                throw new Exception("Selections must be anonymous types without a constructor.");
            }

            // Binary
            if (e is BinaryExpression b)
            {
                return TransformBinaryExpression(b);
            }

            // Unary 
            if (e is UnaryExpression u)
            {
                return TransformUnaryExpression(u);
            }

            // Method call
            if (e is MethodCallExpression mc)
            {
                return TransformMethodCallExpression(mc);
            }

            // Constant
            if (e is ConstantExpression c)
            {
                if (c.Value == null)
                {
                    return "null";
                }
                else if (c.Type == typeof(string))
                {
                    return $"\"{c.Value.ToString()}\"";
                }
                else if (c.Type == typeof(int) ||
                    c.Type == typeof(int?) ||
                    c.Type == typeof(double) ||
                    c.Type == typeof(double?) ||
                    c.Type == typeof(float) ||
                    c.Type == typeof(float?) ||
                    c.Type == typeof(short) ||
                    c.Type == typeof(short?) ||
                    c.Type == typeof(byte) ||
                    c.Type == typeof(byte?) ||
                    c.Type == typeof(decimal) ||
                    c.Type == typeof(decimal?)
                )
                {
                    return $"{String.Format(CultureInfo.InvariantCulture, "{0}", c.Value).ToLower()}";
                }
                else if (c.Type == typeof(bool) ||
                        c.Type == typeof(bool?)
                        )
                {
                    return $"{String.Format(CultureInfo.InvariantCulture, "{0}", c.Value).ToLower()}";
                }
                else if (c.Type == typeof(DateTime) || c.Type == typeof(DateTime?))
                {
                    var dt = (DateTime)c.Value;
                    if (dt == dt.Date) //No time component
                    {
                        return $"\"{dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}\"";
                    }
                    else
                    {
                        return $"\"{dt.ToString("O", CultureInfo.InvariantCulture)}\"";
                    }
                }
                else if (c.Type == typeof(DateTimeOffset) || c.Type == typeof(DateTimeOffset?))
                {
                    return $"\"{((DateTimeOffset)c.Value).ToString("O", CultureInfo.InvariantCulture)}\"";
                }
                return $"\"{c.Value.ToString()}\"";
            }

            throw new Exception($"Operands of type {e.GetType()} and nodeType {e.NodeType} not supported. ");
        }

        protected static List<string> GetPropertyProjectionList(Type type)
        {
            var props = type.GetProperties().Where(p => p.CanWrite);
            var propNames = new List<string>();
            foreach (var prop in props)
            {
                if (prop.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)
                {

                    var jsonName = (prop.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName ?? prop.Name.ToCamelCase();
                    propNames.Add(jsonName);
                }                
            }
            return propNames;
        }

        /// <summary>
        /// Generates a projection for included / joined types using reflection.
        /// Supports Lists and also nested objects of type SanityReference<> or IEnumerable<SanityReference>
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static string GetJoinProjection(string fieldName, Type propertyType)
        {
            string projection = "";

            var isSanityReferenceType = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(SanityReference<>);
            if (isSanityReferenceType)
            {
                // CASE 1: SanityReference<T>
                var fields = GetPropertyProjectionList(propertyType.GetGenericArguments()[0]);
                var fieldList = fields.Aggregate((c, n) => c + "," + n);
                projection = $"{fieldName}->{{ {fieldList} }}";
            }
            else
            {
                var listOfSanityReferenceType = propertyType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) && i.GetGenericArguments()[0].IsGenericType && i.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(SanityReference<>));
                bool isListOfSanityReference = listOfSanityReferenceType != null;
                if (isListOfSanityReference)
                {
                    // CASE 2: IEnumerable<SanityReference<T>>
                    var elementType = listOfSanityReferenceType.GetGenericArguments()[0].GetGenericArguments()[0];
                    var fields = GetPropertyProjectionList(elementType);
                    var fieldList = fields.Aggregate((c, n) => c + "," + n);
                    projection = $"{fieldName}[]->{{ {fieldList} }}";
                }
                else
                {
                    var nestedProperties = propertyType.GetProperties();
                    var sanityImageAssetProperty = nestedProperties.FirstOrDefault(p => !p.PropertyType.IsGenericType && (p.Name.ToLower() == "asset" || ((p.GetCustomAttributes<JsonPropertyAttribute>(true).FirstOrDefault())?.PropertyName?.Equals("asset")).GetValueOrDefault()));
                    bool isSanityImage = sanityImageAssetProperty != null;
                    if (isSanityImage)
                    {
                        // CASE 3: Image.Asset
                        //var propertyName = nestedProperty.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? nestedProperty.Name.ToCamelCase();
                        var fields = GetPropertyProjectionList(propertyType);
                        var nestedFields = GetPropertyProjectionList(sanityImageAssetProperty.PropertyType);

                        // Nested Reference
                        var fieldList = fields.Select(f => f == "asset" ? $"asset->{(nestedFields.Count > 0 ? ("{" + nestedFields.Aggregate((a, b) => a + "," + b) + "}") : "")}" : f).Aggregate((c, n) => c + "," + n);
                        projection = $"{fieldName}{{ {fieldList} }}";
                    }
                    else
                    {
                        var nestedSanityReferenceProperty = nestedProperties.FirstOrDefault(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(SanityReference<>));
                        bool isNestedSanityReferenceType = nestedSanityReferenceProperty != null;
                        if (isNestedSanityReferenceType)
                        {
                            // CASE 4: Property->SanityReference<T> (generalization of Case 3)
                            var propertyName = nestedSanityReferenceProperty.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? nestedSanityReferenceProperty.Name.ToCamelCase();
                            var fields = GetPropertyProjectionList(propertyType);
                            var elementType = nestedSanityReferenceProperty.PropertyType.GetGenericArguments()[0];
                            var nestedFields = GetPropertyProjectionList(elementType);

                            // Nested Reference
                            var fieldList = fields.Select(f => f == propertyName ? $"{propertyName}->{(nestedFields.Count > 0 ? ("{" + nestedFields.Aggregate((a, b) => a + "," + b) + "}") : "")}" : f).Aggregate((c, n) => c + "," + n);
                            projection = $"{fieldName}{{ {fieldList} }}";

                        }
                        else
                        {

                            var nestedListOfSanityReferenceType = nestedProperties.FirstOrDefault(p => p.PropertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) && i.GetGenericArguments()[0].IsGenericType && i.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(SanityReference<>)));
                            bool isNestedListOfSanityReferenceType = nestedListOfSanityReferenceType != null;
                            if (isNestedListOfSanityReferenceType)
                            {
                                // CASE 5: Property->List<SanityReference<T>>
                                var propertyName = nestedListOfSanityReferenceType.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? nestedListOfSanityReferenceType.Name.ToCamelCase();
                                var fields = GetPropertyProjectionList(propertyType);
                                var elementType = nestedListOfSanityReferenceType.PropertyType.GetGenericArguments()[0].GetGenericArguments()[0];
                                var nestedFields = GetPropertyProjectionList(elementType);

                                // Nested Reference
                                var fieldList = fields.Select(f => f == propertyName ? $"{propertyName}[]->{(nestedFields.Count > 0 ? ("{" + nestedFields.Aggregate((a, b) => a + "," + b) + "}") : "")}" : f).Aggregate((c, n) => c + "," + n);
                                projection = $"{fieldName}{{ {fieldList} }}";

                            } 
                            else
                            {
                                
                                var listOfSanityImagesType = propertyType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) && i.GetGenericArguments()[0].GetProperties().Any(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(SanityReference<>) && (p.Name.ToLower() == "asset" || ((p.GetCustomAttributes<JsonPropertyAttribute>(true).FirstOrDefault())?.PropertyName?.Equals("asset")).GetValueOrDefault())));
                                bool isListOfSanityImages = listOfSanityImagesType != null;
                                if (isListOfSanityImages)
                                {
                                    // CASE 6: Array of objects with "asset" field (e.g. images)
                                    var elementType = listOfSanityImagesType.GetGenericArguments()[0];
                                    var fields = GetPropertyProjectionList(elementType);


                                    // Nested Reference
                                    var fieldList = fields.Select(f => f == "asset" ? $"asset->{{ ... }}" : f).Aggregate((c, n) => c + "," + n);
                                    projection = $"{fieldName}[] {{ {fieldList} }}";
                                }
                            }

                        }
                    }
                }
            }

            // CASE 7: Fallback case: not nested / not stongly typed
            if (string.IsNullOrEmpty(projection))
            {
                var enumerableType = propertyType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                var isEnumerable = enumerableType != null;
                if (isEnumerable)
                {
                    var elemType = enumerableType.GetGenericArguments()[0];
                    var fields = GetPropertyProjectionList(elemType);
                    if (fields.Count > 0)
                    {
                        // Other strongly typed includes
                        var fieldList = fields.Aggregate((c, n) => c + "," + n);
                        projection = $"{fieldName}[]->{{ {fieldList} }}";
                    }
                    else
                    {
                        // "object" without any fields defined
                        projection = $"{fieldName}[]->";
                    }
                }
                else
                {
                    var fields = GetPropertyProjectionList(propertyType);
                    if (fields.Count > 0)
                    {
                        // Other strongly typed includes
                        var fieldList = fields.Aggregate((c, n) => c + "," + n);
                        projection = $"{fieldName}->{{ {fieldList} }}";
                    }
                    else
                    {
                        // "object" without any fields defined
                        projection = $"{fieldName}->";
                    }
                }
            }

            return projection;

        }


        internal class SanityQueryBuilder
        {
            public List<string> Constraints { get; } = new List<string>();
            public string Projection { get; set; } = "";
            public string AggregateFunction { get; set; } = "";
            public Type DocType { get; set; } = null;
            public Type ResultType { get; set; } = null;

            public List<string> Orderings { get; set; } = new List<string>();
            public int Take { get; set; } = 0;
            public int Skip { get; set; } = 0;
            public Dictionary<string,string> Includes { get; set; } = new Dictionary<string, string>();

            public virtual string Build(bool includeProjections)
            {
                var sb = new StringBuilder();
                // Select all
                sb.Append("*");

                // Add document type contraint
                if (DocType != null && DocType != typeof(object) && DocType != typeof(SanityDocument))
                {
                    var rootTypeName = DocType.GetSanityTypeName();
                    try
                    {
                        var dummyDoc = Activator.CreateInstance(DocType);
                        var typeName = dummyDoc.SanityType();
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            rootTypeName = typeName;
                        }
                    }
                    catch { }
                    Constraints.Insert(0, $"_type == \"{rootTypeName}\"");
                }

                // Add contraints
                if (Constraints.Count > 0)
                {
                    sb.Append("[");
                    sb.Append(Constraints.Aggregate((c, n) => $"({c}) && ({n})"));
                    sb.Append("]");
                }

                if (includeProjections)
                {
                    var projection = Projection;

                    // Attribute based includes ([Include])
                    // TODO: Note similar logic in TransformMethodCallExpression -- could be refactored to consolidate
                    if (DocType != null)
                    {
                        var properties = DocType.GetProperties();
                        var includedProps = properties.Where(p => p.GetCustomAttributes<IncludeAttribute>(true).FirstOrDefault() != null).ToList();
                        foreach (var prop in includedProps)
                        {
                            var name = (prop.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName ?? prop.Name.ToCamelCase();
                            if (!Includes.ContainsKey(name))
                            {
                                Includes.Add(name, GetJoinProjection(name, prop.PropertyType));
                            }
                        }
                    }


                    // Add joins / includes
                    if (Includes.Count > 0 && string.IsNullOrEmpty(projection))
                    {
                        // Joins require an explicit projection
                        var propertyList = GetPropertyProjectionList(ResultType);
                        if (propertyList.Count > 0)
                        {
                            // Strongly typed case
                            projection = propertyList.Aggregate((c, n) => c + "," + n);
                        }
                        else
                        {
                            // "object" case - include only "includes" / "joins" by default.
                            projection = Includes.Keys.Aggregate((c, n) => c + "," + n);
                        }

                    }

                    // Add projection
                    if (!string.IsNullOrEmpty(projection))
                    {
                        if (Includes.Count > 0)
                        {
                            // Replace fields in projection with an expanded projection representing field names.
                            // e.g from { name, mainImage, organization } to { name, mainImage { asset-> }, organization-> }
                            //     given that mainImage is an asset and organization is a reference
                            projection = projection.Split(',').Select(f => f.Trim())
                                .Select(f => Includes.ContainsKey(f) ? Includes[f] : f)
                                .Aggregate((c, n) => c + "," + n);
                        }

                        sb.Append($"{{ {projection} }}");
                    }
                }


                // Add orderings
                if (Orderings.Count > 0)
                {
                    sb.Append(" | order(" + Orderings.Aggregate((c, n) => $"{c}, {n}") + ")");
                }

                // Add slices
                if (Take > 0)
                {
                    if (Take == 1)
                    {
                        sb.Append($" [{Skip}]");
                    }
                    else
                    {
                        sb.Append($" [{Skip}..{Skip + Take - 1}]");
                    }
                }
                else
                {
                    if (Skip > 0)
                    {
                        sb.Append($" [{Skip}..{int.MaxValue}]");
                    }
                }

                // Wrap with Aggregate function
                if (!string.IsNullOrEmpty(AggregateFunction))
                {
                    sb.Insert(0, AggregateFunction + "(");
                    sb.Append(")");
                }

                return sb.ToString();
            }
        }

    }
}
