using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PropertyBinder.Experiments
{
    public class Base : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
            Binder.Attach(this);
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
            Binder.Attach(this);
        }

        public Source Source { get; } = new Source();

        public double? Aggregate { get; set; }

        public string FormattedAggregate { get; set; }
    }
}
