// Skeleton implementation written by Joe Zachary for CS 3500, September 2013.
// Version 1.1 (Fixed error in comment for RemoveDependency.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// (Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.)
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    //
    // For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    //     dependents("a") = {"b", "c"}
    //     dependents("b") = {"d"}
    //     dependents("c") = {}
    //     dependents("d") = {"d"}
    //     dependees("a") = {}
    //     dependees("b") = {"a"}
    //     dependees("c") = {"a"}
    //     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        private Dictionary<string, HashSet<string>> dependents;
        private Dictionary<string, HashSet<string>> dependees;
                
        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
            dependents = new Dictionary<string, HashSet<string>>();
            dependees = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            get 
            {
                int num = 0;
                foreach (KeyValuePair<string, HashSet<string>> kvp in dependents)
                {
                    foreach (string dependee in kvp.Value)
                    {
                        num++;
                    }
                }
                return num;
            }
        }

        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {  
            get 
            {
                HashSet<string> getDependents;
                if (dependees.TryGetValue(s, out getDependents))
                {
                    return getDependents.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {
            if (dependents.ContainsKey(s)) 
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            if (dependees.ContainsKey(s)) 
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            HashSet<string> getDependents;
            if (dependents.TryGetValue(s, out getDependents))
            {
                return getDependents;
            }
            return new HashSet<string>();
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            HashSet<string> getDependees;
            if (dependees.TryGetValue(s, out getDependees))
            {
                return getDependees;
            }
            return new HashSet<string>();
        }


        /// <summary>
        /// Adds the ordered pair (s,t), if it doesn't exist
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void AddDependency(string s, string t)
        {
            if (!HasDependents(s))
            {
                HashSet<string> temp = new HashSet<string>();
                temp.Add(t);
                dependents.Add(s, temp);
            }
            else 
            {
                HashSet<string> getDependees;
                if (dependents.TryGetValue(s, out getDependees))
                {
                    if (!getDependees.Contains(t))
                    {
                        getDependees.Add(t);
                    }
                }
            }

            if (!HasDependees(t))
            {
                HashSet<string> temp2 = new HashSet<string>();
                temp2.Add(s);
                dependees.Add(t, temp2);
            }
            else
            {
                HashSet<string> getDependents;
                if (dependees.TryGetValue(t, out getDependents))
                {
                    if (!getDependents.Contains(s))
                    {
                        getDependents.Add(s);
                    }
                }
            }
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            HashSet<string> getDependents;
            if (dependents.TryGetValue(s, out getDependents))
            {
                getDependents.Remove(t);
            }

            HashSet<string> getDependees;
            if (dependees.TryGetValue(t, out getDependees))
            {
                getDependees.Remove(s);
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            // Remove all dependees from dependent 's' and add in newDependents
            
            HashSet<string> getDependees;
            if (dependents.TryGetValue(s, out getDependees))
            {
                string[] oldDependees = getDependees.ToArray();
                getDependees.Clear();

                foreach (string newDependent in newDependents)
                {
                    getDependees.Add(newDependent);
                }
                // Remove dependent from all dependees in dependees dictionary
                foreach (string oldDependee in oldDependees)
                {
                    HashSet<string> oldDependeeHash;
                    if(dependees.TryGetValue(oldDependee, out oldDependeeHash)) 
                    {
                        oldDependeeHash.Remove(s);
                    }
                }
            }

            foreach (string newDependent in newDependents)
            {
                AddDependency(s, newDependent);
            }
        }

        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            // Remove all dependents from dependee 's' and add in newDependees

            HashSet<string> getDependents;
            if (dependees.TryGetValue(s, out getDependents))
            {
                string[] oldDependents = getDependents.ToArray();
                getDependents.Clear();

                foreach (string newDependent in newDependees)
                {
                    getDependents.Add(newDependent);
                }
                // Remove dependent from all dependees in dependees dictionary
                foreach (string oldDependent in oldDependents)
                {
                    HashSet<string> oldDependentHash;
                    if (dependents.TryGetValue(oldDependent, out oldDependentHash))
                    {
                        oldDependentHash.Remove(s);
                    }
                }
            }

            foreach (string newDependee in newDependees)
            {
                AddDependency(newDependee, s);
            }
        }
    }
}


