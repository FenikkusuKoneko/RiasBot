using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class CalculatorCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            public CalculatorCommands(CommandHandler ch, CommandService service)
            {
                _ch = ch;
                _service = service;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Calculator([Remainder]string expression)
            {
                try
                {
                    var expr = new NCalc.Expression(expression, NCalc.EvaluateOptions.IgnoreCase);
                    expr.EvaluateParameter += Expr_EvaluateParameter;
                    var result = expr.Evaluate();
                    if (expr.Error == null)
                        await Context.Channel.SendConfirmationEmbed(result.ToString()).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed("Invalid expression!").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task CalcOps()
            {
                var selection = typeof(Math).GetTypeInfo()
                        .GetMethods()
                        .Distinct(new MethodInfoEqualityComparer())
                        .Select(x => x.Name)
                        .Except(new[]
                        {
                        "ToString",
                        "Equals",
                        "GetHashCode",
                        "GetType"
                        });

                await Context.Channel.SendConfirmationEmbed(String.Join(", ", selection)).ConfigureAwait(false);
            }

            private static void Expr_EvaluateParameter(string name, NCalc.ParameterArgs args)
            {
                switch (name.ToLowerInvariant())
                {
                    case "pi":
                        args.Result = Math.PI;
                        break;
                    case "e":
                        args.Result = Math.E;
                        break;
                }
            }

            private class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
            {
                public bool Equals(MethodInfo x, MethodInfo y) => x.Name == y.Name;

                public int GetHashCode(MethodInfo obj) => obj.Name.GetHashCode();
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Det(int n, [Remainder] string rows_collumns)
            {
                string[] rc = new string[n * n];
                rc = rows_collumns.Split(",").ToArray();

                double[,] matrix = new double[n, n];

                int pos = 0;

                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        matrix[i, j] = Convert.ToDouble(rc[pos]);
                        pos++;
                    }
                }

                LUMatrix.LU(matrix, n);

                double l = 1;
                double u = 1;

                for (int lu = 0; lu < n; lu++)
                {
                    l *= LUMatrix.l[lu, lu];
                    u *= LUMatrix.u[lu, lu];
                }

                await ReplyAsync("Determinant = " + (l * u).ToString());
            }
        }
    }
}
