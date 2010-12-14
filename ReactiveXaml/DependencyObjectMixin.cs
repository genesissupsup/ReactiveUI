﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections.Specialized;
using System.Concurrency;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;

#if WINDOWS_PHONE
using Microsoft.Phone.Reactive;
#endif

namespace ReactiveXaml
{
    public static class DependencyPropertyMixin
    {
        public static IObservable<ObservedChange<TObj, TRet>> ObservableFromDP<TObj, TRet>(this TObj This, Expression<Func<TObj, TRet>> Property)
            where TObj : FrameworkElement
        {
            Contract.Requires(This != null);

            // Track down the DP for this property
            var prop_name = RxApp.expressionToPropertyName(Property);
            var fi = typeof(TObj).GetField(prop_name + "Property", BindingFlags.Public | BindingFlags.Static);
            var dp = fi.GetValue(This) as DependencyProperty;

            return new ObservableFromDPHelper<TObj, TRet>(This, dp, prop_name);
        }

        class ObservableFromDPHelper<TObj,TRet> : IObservable<ObservedChange<TObj,TRet>>
            where TObj : FrameworkElement
        {
            TObj source;
            string propName;
            PropertyInfo propGetter;
            Subject<ObservedChange<TObj, TRet>> subject = new Subject<ObservedChange<TObj, TRet>>();

            public ObservableFromDPHelper(TObj dobj, DependencyProperty dp, string propName)
            {
                var b = new Binding(propName) { Source = dobj };
                var prop = System.Windows.DependencyProperty.RegisterAttached(
                    "ListenAttached" + propName + this.GetHashCode().ToString("{0:x}"),
                    typeof(object),
                    typeof(TObj),
                    new PropertyMetadata(new PropertyChangedCallback(onPropertyChanged)));

                source = dobj;
                this.propName = propName;
                propGetter = typeof(TObj).GetProperty(propName);
                dobj.SetBinding(prop, b);
            }

            void onPropertyChanged(DependencyObject Sender, DependencyPropertyChangedEventArgs args)
            {
                subject.OnNext(new ObservedChange<TObj, TRet>() { 
                    PropertyName = propName, 
                    Sender = source,
                    Value = (TRet)propGetter.GetValue(Sender, null)
                });
            }

            public IDisposable Subscribe(IObserver<ObservedChange<TObj, TRet>> observer)
            {
                return subject.Subscribe(observer);
            }
        }
    }
}

// vim: tw=120 ts=4 sw=4 et :