using System;
using Keystone.Extensions;


namespace KeyScript.Rules
{
    public class RuleSet
    {
        private Rule[] mRules;

        public Rule[] Rules { get { return mRules; } }

        public void AddRule(Rule rule)
        {
            
           mRules = mRules.ArrayAppend(rule);
        }
    }

    // rules are not nodes.  
    public abstract class Rule
    {
        // http://developer.apple.com/library/ios/#documentation/cocoa/Conceptual/ErrorHandlingCocoa/ErrorObjectsDomains/ErrorObjectsDomains.html

        protected int _errorCode;
        // protected int _ruleDescription;
        // protected int _recoverySuggestion;
        // protected in _recoveryOptionFlags; // abort/retry/cancel to show up in a message dialog

        /// <summary>
        /// ErrorCode which application can then use against an array of localized
        /// string descriptions that are read from a file.
        /// </summary>
        public virtual int ErrorCode
        {
            get { return _errorCode; }
            set { _errorCode = value; }
        }
        
        // validates this rule on the relevant property of the Entity instance
        public abstract bool Validate(string entityID, object[] args);
    }



    public class SimpleRule : Rule
    {
        public delegate bool RuleDelegate(string entityID, object[] args);
        
        private RuleDelegate _ruleDelegate;

        public SimpleRule(int errorCode, RuleDelegate ruleDelegate)
        {
            _errorCode = errorCode;
            _ruleDelegate = ruleDelegate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Can be null.</param>
        /// <returns></returns>
        public override bool Validate(string entityID, object[] args)
        {
            return _ruleDelegate(entityID, args);
        }
        
    }
}
