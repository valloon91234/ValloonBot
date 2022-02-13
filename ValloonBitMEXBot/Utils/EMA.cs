using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Trading.Utils
{
    class EMA
    {
        private readonly float _alpha;
        private float _lastAverage = float.NaN;

        public EMA(int lookBack) => _alpha = 2f / (lookBack + 1);

        public float NextValue(float value) => _lastAverage = float.IsNaN(_lastAverage)
            ? value
            : (value - _lastAverage) * _alpha + _lastAverage;
    }
}
