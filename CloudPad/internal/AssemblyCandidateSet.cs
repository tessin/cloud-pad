using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloudPad.Internal {
  class AssemblyCandidate {
    public string FullName => Name.FullName;
    public AssemblyName Name { get; set; }
    public string Location { get; set; }
    public string Source { get; set; }
  }

  class AssemblyCandidateSet {
    private readonly Dictionary<string, List<AssemblyCandidate>> _d = new Dictionary<string, List<AssemblyCandidate>>(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<KeyValuePair<string, List<AssemblyCandidate>>> Set {
      get {
        return _d.OrderBy(x => x.Key); // sort is just to make debugging easier
      }
    }

    public void Add(string location, AssemblyName name, string source) {
      if (!_d.TryGetValue(name.Name, out var list)) {
        _d.Add(name.Name, list = new List<AssemblyCandidate>());
      }

      var candidiate = list.Find(c => c.FullName == name.FullName);
      if (candidiate == null) {
        list.Add(new AssemblyCandidate { Location = location, Name = name, Source = source });
      }
    }

    public bool Unref(AssemblyName name) {
      if (_d.TryGetValue(name.Name, out var list)) {
        var candidiate = list.FindIndex(c => c.FullName == name.FullName);
        if (candidiate != -1) {
          list.RemoveAt(candidiate);
          if (list.Count == 0) {
            _d.Remove(name.Name);
          }
          return true;
        }
      }
      return false;
    }
  }
}
