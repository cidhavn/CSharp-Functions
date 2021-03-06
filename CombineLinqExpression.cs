using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Extension
{
    // 參考: http://stackoverflow.com/questions/6180704/combine-several-similar-select-expressions-into-a-single-expression
    
    public static class ExpressionExtension
    {
        /// <summary>
        /// 合併 LINQ Expression
        /// </summary>
        public static Expression<Func<TSource, TDestination>> Combine<TSource, TDestination>(
            this Expression<Func<TSource, TDestination>> source,
            params Expression<Func<TSource, TDestination>>[] selectors)
        {
            var zeroth = ((MemberInitExpression)source.Body);
            var param = source.Parameters[0];

            List<MemberBinding> bindings = new List<MemberBinding>(zeroth.Bindings.OfType<MemberAssignment>());

            for (int i = 0; i < selectors.Length; i++)
            {
                var memberInit = (MemberInitExpression)selectors[i].Body;
                var replace = new ParameterReplaceVisitor(selectors[i].Parameters[0], param);

                foreach (var binding in memberInit.Bindings.OfType<MemberAssignment>())
                {
                    if (bindings.Any(x => x.Member.Name == binding.Member.Name))
                    {
                        bindings.Remove(bindings.First(x => x.Member.Name == binding.Member.Name));
                    }

                    bindings.Add(Expression.Bind(binding.Member, replace.VisitAndConvert(binding.Expression, "Combine")));
                }
            }

            return Expression.Lambda<Func<TSource, TDestination>>(Expression.MemberInit(zeroth.NewExpression, bindings), param);
        }

        private class ParameterReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression from, to;

            public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
            {
                this.from = from;
                this.to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == from ? to : base.VisitParameter(node);
            }
        }

        #region Sample

        public static void Test()
        {
            Expression<Func<Book, Book>> expression1 = x => new Book
            {
                Id = 1,
                Price = 199m
            };

            Expression<Func<Book, Book>> expression2 = x => new Book
            {
                Price = 299m,
                Name = "AAA"
            };

            var newExpression = expression1.Combine(expression2);
            var func = newExpression.Compile();
            Console.WriteLine(func(new Book()));
        }

        private class Book
        {
            public int Id { get; set; }
            public decimal Price { get; set; }
            public string Name { get; set; }
        }

        #endregion Sample
    }
}
