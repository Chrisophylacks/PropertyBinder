using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PropertyBinder.Experiments
{
    public class Base : INotifyPropertyChanged, IDisposable
    {
        private List<IDisposable> anchors = new List<IDisposable>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            foreach (var anchor in anchors)
            {
                anchor.Dispose();
            }
            anchors.Clear();
        }

        protected T Anchor<T>(T value)
            where T : IDisposable
        {
            anchors.Add(value);
            return value;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Model : Base
    {
        public double? Data1 { get; set; }

        public double? Data2 { get; set; }

        public double? Data3 { get; set; }

        public double? Data4 { get; set; }

        public double? Data5 { get; set; }

        public double? Data6 { get; set; }

        public double? Data7 { get; set; }

        public double? Data8 { get; set; }

        public double? Data9 { get; set; }

        public double? Data10 { get; set; }
    }

    public class Source : Base
    {
        static Binder<Source> Binder = new Binder<Source>();

        static Source()
        {
            Binder.Bind(x => x.Model.Data1).PropagateNullValues().To(x => x.Data1);
            Binder.Bind(x => x.Model.Data2).PropagateNullValues().To(x => x.Data2);
            Binder.Bind(x => x.Model.Data3).PropagateNullValues().To(x => x.Data3);
            Binder.Bind(x => x.Model.Data4).PropagateNullValues().To(x => x.Data4);
            Binder.Bind(x => x.Model.Data5).PropagateNullValues().To(x => x.Data5);
            Binder.Bind(x => x.Model.Data6).PropagateNullValues().To(x => x.Data6);
            Binder.Bind(x => x.Model.Data7).PropagateNullValues().To(x => x.Data7);
            Binder.Bind(x => x.Model.Data8).PropagateNullValues().To(x => x.Data8);
            Binder.Bind(x => x.Model.Data9).PropagateNullValues().To(x => x.Data9);
            Binder.Bind(x => x.Model.Data10).PropagateNullValues().To(x => x.Data10);
        }

        public Source()
        {
            Anchor(Binder.Attach(this));
        }

        public Model Model { get; set; }

        public double? Data1 { get; set; }

        public double? Data2 { get; set; }

        public double? Data3 { get; set; }

        public double? Data4 { get; set; }

        public double? Data5 { get; set; }

        public double? Data6 { get; set; }

        public double? Data7 { get; set; }

        public double? Data8 { get; set; }

        public double? Data9 { get; set; }

        public double? Data10 { get; set; }
    }

    public class Consumer : Base
    {
        static readonly Binder<Consumer> Binder = new Binder<Consumer>();

        static Consumer()
        {
            Binder.Bind(x => x.Source.Data1 + x.Source.Data2 + x.Source.Data3 + x.Source.Data4 + x.Source.Data5
                + x.Source.Data6 + x.Source.Data7 + x.Source.Data8 + x.Source.Data9 + x.Source.Data10).To(x => x.Aggregate);
            Binder.Bind(x => string.Format(CultureInfo.InvariantCulture, "{0:#.0}", x.Aggregate)).To(x => x.FormattedAggregate);
        }

        public Consumer()
        {
            Anchor(Source);
            Anchor(Binder.Attach(this));
        }

        public Source Source { get; } = new Source();

        public double? Aggregate { get; set; }

        public string FormattedAggregate { get; set; }
    }

    public class ExplosiveModel : Base
    {
        static readonly Binder<ExplosiveModel> Binder = new Binder<ExplosiveModel>();

        static ExplosiveModel()
        {
            Binder.Bind(x => x.Consumer.FormattedAggregate).To(x => x.Aggregate);
        }

        public ExplosiveModel(int size)
        {
            Consumer = Anchor(new Consumer());
            for (int i = 0; i < size; ++i)
            {
                Children.Add(Anchor(new ExplosiveModel(size - 1)));
            }

            Anchor(Binder.Attach(this));
        }

        public Consumer Consumer { get; }

        public string Aggregate { get; private set; }

        public ObservableCollection<ExplosiveModel> Children { get; } = new ObservableCollection<ExplosiveModel>();

        public void ModifyAll()
        {
            Consumer.Source.Model = new Model();
            foreach (var child in Children)
            {
                child.ModifyAll();
            }
        }
    }

}
