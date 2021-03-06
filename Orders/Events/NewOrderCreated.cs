﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Orders.ViewModels;
using Orders.CommonTypes;

namespace Orders.Events
{
    /// <summary>
    /// Message about fact that new Order saved in DataBase
    /// </summary>
    public class NewOrderCreated : PubSubEvent<int> { }
}
