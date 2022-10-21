using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi.V1;

namespace TinkoffAPIConsole.ModelAdditional
{
    internal class ClosePricesList
    {
        public List<InstrumentClosePriceResponse> closeprices { get; set; }
    }
}
