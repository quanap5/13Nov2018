using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;

namespace Xisom.OCR.Draw
{
    //An interface contain definitions for a group of related functionality that a class or a struct acan implement
    //To implement an interface member the corresponding member of the implementing class must be public non-static and have 
    //same name and signature as the interface member
    
    public interface IGeometry
    {
        GraphicsPath GraphicsPath { get; }
    }
}
