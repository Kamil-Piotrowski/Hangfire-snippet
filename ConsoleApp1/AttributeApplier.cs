using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class AttributeApplier
    {

        public void addAttribute(Action method)
        {
            Console.WriteLine("applied!");
            method();
        }
    }
}
