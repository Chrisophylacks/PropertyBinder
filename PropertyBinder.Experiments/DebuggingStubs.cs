using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyBinder.Experiments
{
    public class SourceModel : Base
    {
        public int? Value1 { get; set; }

        public int? Value2 { get; set; }

        public int? DefaultValue { get; set; }
    }

    public class UserModel : Base
    {
        private static readonly Binder<UserModel> Binder = new Binder<UserModel>();
        private int? _result;

        static UserModel()
        {
            Binder.BindIf(x => x.SourceType == 1, x => x.Source == null ? null : x.Source.Value1)
                .ElseIf(x => x.SourceType == 2, x => x.Source == null ? null : x.Source.Value2)
                .Else(x => x.Source == null ? null : x.Source.DefaultValue)
                .To(x => x.BaseValue);

            Binder.Bind(x => x.BaseValue + (x.Modifier ?? 0)).To(x => x.Result);

            Binder.Bind(x => x.Result).To((x, v) =>
            {
                Console.WriteLine("Result set to: {0}", x.Result);
            });
        }

        public UserModel()
        {
            Binder.Attach(this);
        }

        public SourceModel Source { get; set; }

        public int? SourceType { get; set; }

        public int? Modifier { get; set; }

        public int? Result
        {
            get { return _result; }
            set { _result = value; }
        }

        private int? BaseValue { get; set; }
    }

}
