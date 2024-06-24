using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Map.Concurrent
{
    class HelpRecord
    {
        int curTid;
        long lastPhase;
        long nextCheck;
        HelpRecord() { curTid = −1; reset(); }
        void reset()
        {
            curTid = (curTid + 1) %
           NUM THRDS;
            lastPhase = state.get(curTid).phase;
            nextCheck = HELPING DELAY;
        }
    }
}
