using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloudPad.Internal {
  class AssemblyCandidate : IComparable<AssemblyCandidate> {
    public string FullName => Name.FullName;
    public AssemblyName Name { get; set; }
    public string Location { get; set; }
    public string Source { get; set; }

    public int CompareTo(AssemblyCandidate other) {
      return this.Name.Version.CompareTo(other.Name.Version); // descending, highest version first
    }
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

      var candidiate = new AssemblyCandidate { Location = location, Name = name, Source = source };
      var index = list.BinarySearch(candidiate);
      if (index < 0) {
        list.Insert(~index, candidiate);
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
