using System;

namespace Lite.Validation.Fluent
{
    // Inherits <T, TElement> so that standard rules (NotNull, etc.) apply to the ELEMENT, not the collection.
    public interface ICollectionRuleBuilder<T, TElement> : IPropertyRuleBuilder<T, TElement>
    {
        // Future: Where(Func<TElement, bool> predicate)
    }
}
