using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.Infrastructure.Patterns.AntiCorruption
{
    /// <summary>
    /// Interface for translating between legacy and modern data models
    /// Part of Anti-Corruption Layer pattern implementation
    /// </summary>
    public interface ILegacyDataTranslator<TLegacy, TModern>
    {
        /// <summary>
        /// Translates legacy data model to modern domain model
        /// </summary>
        Task<TModern> TranslateToModernAsync(TLegacy legacyModel);

        /// <summary>
        /// Translates modern domain model to legacy data format
        /// </summary>
        Task<TLegacy> TranslateToLegacyAsync(TModern modernModel);

        /// <summary>
        /// Batch translation for performance optimization
        /// </summary>
        Task<IEnumerable<TModern>> TranslateBatchToModernAsync(IEnumerable<TLegacy> legacyModels);

        /// <summary>
        /// Validates that the translation is successful and complete
        /// </summary>
        Task<bool> ValidateTranslationAsync(TLegacy legacy, TModern modern);
    }
}