using System;
using System.Linq;

namespace Nela.Ramify {
    public class ViewUtils {
        public static bool ShouldDisableOnMissingViewModel<TViewModel>(View<TViewModel> view) where TViewModel : IViewModel {
            return view.GetType().GetCustomAttributes(typeof(DisableOnMissingViewModelAttribute), true).Any();
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DisableOnMissingViewModelAttribute : Attribute { }
}