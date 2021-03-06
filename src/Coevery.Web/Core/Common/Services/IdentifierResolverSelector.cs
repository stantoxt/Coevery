﻿using System.Collections.Generic;
using System.Linq;
using Coevery.ContentManagement;
using Coevery.Core.Common.Models;

namespace Coevery.Core.Common.Services {
    public class IdentifierResolverSelector : IIdentityResolverSelector {
        private readonly IContentManager _contentManager;

        public IdentifierResolverSelector(IContentManager contentManager) {
            _contentManager = contentManager;
        }

        public IdentityResolverSelectorResult GetResolver(ContentIdentity contentIdentity) {
            if (contentIdentity.Has("Identifier")) {
                return new IdentityResolverSelectorResult {
                    Priority = 5,
                    Resolve = ResolveIdentity
                };
            }

            return null;
        }

        private IEnumerable<ContentItem> ResolveIdentity(ContentIdentity identity) {
            var identifier = identity.Get("Identifier");

            if (identifier == null) {
                return null;
            }

            var comparer = new ContentIdentity.ContentIdentityEqualityComparer();
            return _contentManager
                .Query<IdentityPart, IdentityPartRecord>()
                .Where(p => p.Identifier == identifier)
                .List<ContentItem>()
                .Where(c => comparer.Equals(identity, _contentManager.GetItemMetadata(c).Identity));
        }
    }
}