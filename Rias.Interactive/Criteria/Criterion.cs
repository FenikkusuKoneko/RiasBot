using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Rias.Interactive.Criteria
{
    public class Criterion<T> : ICriterion<T>
    {
        private readonly List<ICriterion<T>> _criterions = new List<ICriterion<T>>();

        public Criterion<T> AddCriterion(ICriterion<T> criterion)
        {
            _criterions.Add(criterion);
            return this;
        }

        public async Task<bool> CheckAsync(IMessage message, T parameter)
        {
            foreach (var criterion in _criterions)
            {
                var result = await criterion.CheckAsync(message, parameter);
                if (!result)
                    return false;
            }

            return true;
        }
    }
}