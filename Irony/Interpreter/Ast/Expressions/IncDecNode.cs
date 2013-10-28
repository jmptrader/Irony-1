﻿#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Irony.Parsing;
using Irony.Interpreter;

namespace Irony.Interpreter.Ast {

  public class IncDecNode : AstNode {
    public bool IsPostfix;
    public string OpSymbol;
    public string BinaryOpSymbol; //corresponding binary operation: + for ++, - for --
    public ExpressionType BinaryOp; 
    public AstNode Argument;
    private OperatorImplementation _lastUsed;

    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      FindOpAndDetectPostfix(context, treeNode); 
      int argIndex = IsPostfix? 0 : 1;
      Argument = AddChild(NodeUseType.ValueReadWrite, "Arg", treeNode.ChildNodes[argIndex]);
      BinaryOpSymbol = OpSymbol[0].ToString(); //take a single char out of ++ or --
      BinaryOp = context.GetOperatorExpressionType(BinaryOpSymbol); 
      base.AsString = OpSymbol + (IsPostfix ? "(postfix)" : "(prefix)");
    }

    private void FindOpAndDetectPostfix(ParsingContext context, ParseTreeNode treeNode) {
      IsPostfix = false; //assume it 
      OpSymbol = treeNode.ChildNodes[0].FindTokenAndGetText();
      if (OpSymbol == "--" || OpSymbol == "++") return;
      IsPostfix = true; 
      OpSymbol = treeNode.ChildNodes[1].FindTokenAndGetText();
      if (OpSymbol == "--" || OpSymbol == "++") return;
      //report error
      throw new AstException(this, Resources.ErrInvalidArgsForIncDec);
    }

    protected override object DoEvaluate(ScriptThread thread) {
      thread.CurrentNode = this;  //standard prolog
      var oldValue = Argument.Evaluate(thread);
      var newValue = thread.Runtime.ExecuteBinaryOperator(BinaryOp, oldValue, 1, ref _lastUsed);
      Argument.SetValue(thread, newValue);
      var result = IsPostfix ? oldValue : newValue;
      thread.CurrentNode = Parent; //standard epilog
      return result; 
    }

    public override void SetIsTail() {
      base.SetIsTail();
      Argument.SetIsTail(); 
    }
  }//class

}
