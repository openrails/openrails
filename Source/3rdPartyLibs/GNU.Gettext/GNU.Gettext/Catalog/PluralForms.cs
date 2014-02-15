//
// PluralForms.cs
//
// Author:
//   David Makovsk� <yakeen@sannyas-on.net>
//
// Copyright (C) 1999-2006 Vaclav Slavik (Code and design inspiration - poedit.org)
// Copyright (C) 2007 David Makovsk�
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GNU.Gettext
{
/*
                                Simplified Grammar

			Expression:
				LogicalOrExpression '?' Expression ':' Expression
				LogicalOrExpression

			LogicalOrExpression:
				LogicalAndExpression "||" LogicalOrExpression   // to (a || b) || c
				LogicalAndExpression

			LogicalAndExpression:
				EqualityExpression "&&" LogicalAndExpression    // to (a && b) && c
				EqualityExpression

			EqualityExpression:
				RelationalExpression "==" RelationalExperession
				RelationalExpression "!=" RelationalExperession
				RelationalExpression

			RelationalExpression:
				MultiplicativeExpression '>' MultiplicativeExpression
				MultiplicativeExpression '<' MultiplicativeExpression
				MultiplicativeExpression ">=" MultiplicativeExpression
				MultiplicativeExpression "<=" MultiplicativeExpression
				MultiplicativeExpression

			MultiplicativeExpression:
				PmExpression '%' PmExpression
				PmExpression

			PmExpression:
				N
				Number
				'(' Expression ')'
*/
	internal class PluralFormsToken
	{
		public enum Type
		{
			Error,
			Eof,
			Number,
			N,
			Plural,
			Nplurals,
			Equal,
			Assign,
			Greater,
			GreaterOrEqual,
			Less,
			LessOrEqual,
			Reminder,
			NotEqual,
			LogicalAnd,
			LogicalOr,
			Question,
			Colon,
			Semicolon,
			LeftBracket,
			RightBracket
		}

		Type type;
		int number;
		
		public Type TokenType {
			get { return type; }
			set { type = value; }
		}
		
		public int Number {
			get { return number; }
			set { number = value; }
		}
		
		public PluralFormsToken()
		{
		}

		public PluralFormsToken(PluralFormsToken src)
		{
			type = src.type;
			number = src.number;
		}
		
		public override string ToString()
		{
			return string.Format("[Token: Type={0}, Number={1}]", TokenType, Number);
		}
	}
	
	internal class PluralFormsScanner
	{
		string str;
		int pos = 0;
		PluralFormsToken token;
		
		public PluralFormsScanner(string str)
		{
			this.str = str;
			token = new PluralFormsToken();
			NextToken();
		}
		
		public PluralFormsToken  Token {
			get { return token; }
		}

		// returns false if error
		public bool NextToken()
		{
			PluralFormsToken.Type type = PluralFormsToken.Type.Error;			
			while (pos < str.Length && str[pos] == ' ') {
				++pos;
			}
			if (pos >= str.Length || str[pos] == '\0') {
				type = PluralFormsToken.Type.Eof;
			}
			else if (Char.IsDigit(str[pos])) {
				int number = str[pos++] - '0';
				while (pos < str.Length && Char.IsDigit (str[pos])) {
					number = number * 10 + (str[pos++] - '0');
				}
				token.Number = number;
				type = PluralFormsToken.Type.Number;
			}
			else if (Char.IsLetter(str[pos])) {
				int begin = pos++;
				while (pos < str.Length && Char.IsLetterOrDigit (str[pos])) {
					++pos;
				}
				int size = pos - begin;
				if (size == 1 && str[begin] == 'n') {
					type = PluralFormsToken.Type.N;
				}
				else if (size == 6 && str.Substring(begin, size) == "plural") {
					type = PluralFormsToken.Type.Plural;
				}
				else if (size == 8 && str.Substring(begin, size) == "nplurals") {
					type = PluralFormsToken.Type.Nplurals;
				}
			}
			else if (str[pos] == '=') {
				++pos;
				if (pos < str.Length && str[pos] == '=') {
					++pos;
					type = PluralFormsToken.Type.Equal;
				}
				else {
					type = PluralFormsToken.Type.Assign;
				}
			}
			else if (str[pos] == '>') {
				++pos;
				if (pos < str.Length && str[pos] == '=') {
					++pos;
					type = PluralFormsToken.Type.GreaterOrEqual;
				}
				else {
					type = PluralFormsToken.Type.Greater;
				}
			}
			else if (str[pos] == '<') {
				++pos;
				if (pos < str.Length && str[pos] == '=') {
					++pos;
					type = PluralFormsToken.Type.LessOrEqual;
				}
				else {
					type = PluralFormsToken.Type.Less;
				}
			}
			else if (str[pos] == '%') {
				++pos;
				type = PluralFormsToken.Type.Reminder;
			}
			else if (str[pos] == '!' && str[pos + 1] == '=') {
				pos += 2;
				type = PluralFormsToken.Type.NotEqual;
			}
			else if (pos + 1 < str.Length && str[pos] == '&' && str[pos + 1] == '&') {
				pos += 2;
				type = PluralFormsToken.Type.LogicalAnd;
			}
			else if (pos + 1 < str.Length && str[pos] == '|' && str[pos + 1] == '|') {
				pos += 2;
				type = PluralFormsToken.Type.LogicalOr;
			}
			else if (str[pos] == '?') {
				++pos;
				type = PluralFormsToken.Type.Question;
			}
			else if (str[pos] == ':') {
				++pos;
				type = PluralFormsToken.Type.Colon;
			}
			else if (str[pos] == ';') {
				++pos;
				type = PluralFormsToken.Type.Semicolon;
			}
			else if (str[pos] == '(') {
				++pos;
				type = PluralFormsToken.Type.LeftBracket;
			}
			else if (str[pos] == ')') {
				++pos;
				type = PluralFormsToken.Type.RightBracket;
			}
			token.TokenType = type;
			return type != PluralFormsToken.Type.Error;
		}
	}
	
	internal class PluralFormsNode
	{
		PluralFormsToken token;
		PluralFormsNode[] nodes = new PluralFormsNode[3];
    
		#region Constructor
		public PluralFormsNode(PluralFormsToken token)
		{
			this.token = token;
		}
		#endregion
		
		public PluralFormsToken Token {
			get { return token; }
		}
		
		public PluralFormsNode Node(int i)
		{
			if (i >= 0 && i <= 2)
				return nodes[i];
			else
				return null;
		}
		
		public PluralFormsNode[] Nodes {
			get { return this.nodes; }
		}
		
		public int NodesCount {
			get { return this.nodes.Length; }
		}
		
		public void SetNode(int i, PluralFormsNode n)
		{
			if (i >= 0 && i <= 2)
				nodes[i] = n;
		}
         
		public PluralFormsNode ReleaseNode(int i)
		{
			PluralFormsNode node = nodes[i];
			nodes[i] = null;
			return node;
		}
		
		RecursiveTracer tracer = new RecursiveTracer();
		internal RecursiveTracer Tracer
		{
			get { return tracer; }
			set	{ tracer = value; }
		}
         
		public long Evaluate(long n)
		{
#if DEBUG
			if (Tracer != null)
			{
				Tracer.Text.AppendFormat("{0}: (n = {1}): ", token.TokenType, n);
			}
#endif
			long n0 = -1, n1 = -1, n2 = -1, result = -1;
			switch (token.TokenType) {
			// leaf
			case PluralFormsToken.Type.Number:
				result = token.Number;
				break;
			case PluralFormsToken.Type.N:
				result = n;
				break;
			// 2 args
			case PluralFormsToken.Type.Equal:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 == n1 ? 1 : 0;
				break;
			case PluralFormsToken.Type.NotEqual:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 != n1 ? 1 : 0;
				break;
			case PluralFormsToken.Type.Greater:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 > n1 ? 1 : 0;
				break;
			case PluralFormsToken.Type.GreaterOrEqual:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 >= n1 ? 1 : 0;
				break;
			case PluralFormsToken.Type.Less:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 < n1 ? 1 : 0;
				break;
			case PluralFormsToken.Type.LessOrEqual:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 <= n1 ? 1 : 0;
				break;
			case PluralFormsToken.Type.Reminder:
				long number = nodes[1].Evaluate(n);
				if (number != 0)
				{
					n0 = nodes[0].Evaluate(n);
					result = n0 % number;
				}
				else
					result = 0;
#if DEBUG
				Tracer.Text.AppendFormat("n0 % number = {0} % {1} = {2} | ", n0, number, result);
#endif
				break;
			case PluralFormsToken.Type.LogicalAnd:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 != 0 && n1 != 0 ? 1 : 0;
				break;
			case PluralFormsToken.Type.LogicalOr:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				result = n0 != 0 || n1 != 0 ? 1 : 0;
				break;
			// 3 args
			case PluralFormsToken.Type.Question:
				n0 = nodes[0].Evaluate(n);
				n1 = nodes[1].Evaluate(n);
				n2 = nodes[2].Evaluate(n);
				result = n0 != 0 ? n1 : n2;
				break;
			default:
				result = 0;
				break;
			}
#if DEBUG
			if (Tracer != null)
			{
				Tracer.Text.AppendFormat("{0}{1}{2} result = {3}",
				                         n0 != -1 ? "n0 = " + n0.ToString() + ", " : "",
				                         n1 != -1 ? ", n1 = " + n1.ToString() + ", " : "",
				                         n2 != -1 ? ", n2 = " + n2.ToString() + ", " : "",
				                         result);
				Tracer.Level--;
			}
#endif
			return result;
		}
		
		public override string ToString()
		{
			return string.Format("[Node: Token={0}]", Token);
		}

		public delegate void IterateNodesDelegate(PluralFormsNode node);
		
		public static void IterateNodes(PluralFormsNode node, IterateNodesDelegate doBefore, IterateNodesDelegate doAfter)
		{
			doBefore(node);
			for(int i = 0; i < node.NodesCount; i++)
			{
				if (node.Nodes[i] != null)
					IterateNodes(node.Nodes[i], doBefore, doAfter);
			}
			doAfter(node);
		}
	}
	
	
	/// <summary>
	/// Plural forms calculator.
	/// </summary>
	public class PluralFormsCalculator
	{
		int nplurals;
		PluralFormsNode plural;
		string expression;
		
		public PluralFormsCalculator(string expression)
		{
			nplurals = 0;
			plural = null;
			this.expression = expression;
		}
		
		public int NPlurals {
			get { return nplurals; }
		}
		
		/// <summary>
        /// Evaluate the specified number, returns msgstr index.
		/// </summary>
		/// <param name='n'>
		/// Number to evaluate
		/// </param>
		/// <param name='traceToFile'>
		/// Debug purposes only. Trace to file an evaluation tree.
		/// </param>
		public long Evaluate(long n, bool traceToFile)
		{
			if (plural == null) {
				return 0;
			}
#if DEBUG
			RecursiveTracer tracer = new RecursiveTracer();
			tracer.Text.AppendFormat("Expression: {0}", expression);
			tracer.Text.AppendLine();
			tracer.Text.AppendFormat("Evaluate: {0}", n);
			tracer.Text.AppendLine();
			tracer.Text.AppendLine();
#endif
			long number = plural.Evaluate(n);
#if DEBUG
			PluralFormsNode.IterateNodes(
				plural,
	            delegate(PluralFormsNode node) 
	            {
					tracer.Text.AppendFormat("{0}: ", tracer.Level++);
					tracer.Indent();
					if (node.Tracer != null)
						tracer.Text.AppendLine(node.Tracer.Text.ToString());
				},
				delegate(PluralFormsNode node) 
				{
					tracer.Level--;
				}
			);
			if (traceToFile)
				tracer.SaveToFile("Evaluations.txt");
#endif
			if (number < 0 || number > nplurals) {
				return 0;
			}
			return number;
		}

		public long Evaluate(long n)
		{
			return Evaluate(n, false);
		}

		/// <summary>
		/// Make the specified str.
		/// Creates evaluator object. Returns null if failed to parse.
		/// </summary>
		/// <param name='str'>
		/// Text after "Plural-Forms:" (e.g. "nplurals=2; plural=(n != 1);").
		/// </param>
		public static PluralFormsCalculator Make(string str)
		{
			if (String.IsNullOrEmpty(str))
				return null;
			if (str.EndsWith("\n"))
				str = str.Remove(str.Length - 1, 1);
			if (str.EndsWith("\\n"))
				str = str.Remove(str.Length - 2, 2);
			if (!str.EndsWith(";"))
				str += ";";
			
			PluralFormsCalculator calculator = new PluralFormsCalculator(str);
			PluralFormsScanner scanner = new PluralFormsScanner(str);
			PluralFormsParser p = new PluralFormsParser(scanner);
			if (!p.Parse(calculator)) {
				return null;
			}
			return calculator;
		}
		
		public void DumpNodes(string fileName)
		{
			if (plural != null)
			{
				RecursiveTracer tracer = new RecursiveTracer();
				tracer.Text.Append(expression);
				tracer.Text.AppendLine();
				
				PluralFormsNode.IterateNodes(
					plural,
		            delegate(PluralFormsNode node) 
		            {
						tracer.Text.AppendFormat("{0}: ", tracer.Level++);
						tracer.Indent();
						tracer.Text.AppendLine(node.ToString());
					},
					delegate(PluralFormsNode node) 
					{
						tracer.Level--;
					}
				);
				tracer.SaveToFile(fileName);
			}
		}

		internal void Init(int nplurals, PluralFormsNode plural)
		{
			this.nplurals = nplurals;
			this.plural = plural;
		}
	}
	
	internal class PluralFormsParser
	{
		// stops at SEMICOLON, returns 0 if error
		PluralFormsScanner scanner;
		
		public PluralFormsParser(PluralFormsScanner scanner)
		{
			this.scanner = scanner;
		}
		
		public bool Parse(PluralFormsCalculator calculator)
		{
			if (Token.TokenType != PluralFormsToken.Type.Nplurals)
				return false;
			if (! NextToken())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Assign)
				return false;
			if (! NextToken())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Number)
				return false;
			int nplurals = Token.Number;
			if (! NextToken())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Semicolon)
				return false;
			if (! NextToken())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Plural)
				return false;
			if (! NextToken())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Assign)
				return false;
			if (! NextToken())
				return false;
			PluralFormsNode plural = ParsePlural();
			if (plural == null)
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Semicolon)
				return false;
			if (! NextToken())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Eof)
				return false;
			calculator.Init(nplurals, plural);
			return true;
		}

		PluralFormsNode ParsePlural()
		{
			PluralFormsNode p = Expression();
			if (p == null) {
				return null;
			}
			if (Token.TokenType != PluralFormsToken.Type.Semicolon) {
				return null;
			}
			return p;
		}
    
		PluralFormsToken Token {
			get { return scanner.Token; }
		}
		
		bool NextToken()
		{
			if (! scanner.NextToken())
				return false;
			return true;
		}

		PluralFormsNode Expression()
		{
			PluralFormsNode p = LogicalOrExpression();
			if (p == null)
				return null;
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Question) {
				PluralFormsNode qn = new PluralFormsNode(new PluralFormsToken(Token));
				if (! NextToken()) {
					return null;
				}
				p = Expression();
				if (p == null) {
					return null;
				}
				qn.SetNode(1, p);
				if (Token.TokenType != PluralFormsToken.Type.Colon) {
					return null;
				}
				if (! NextToken()) {
					return null;
				}
				p = Expression();
				if (p == null) {
					return null;
				}
				qn.SetNode(2, p);
				qn.SetNode(0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode LogicalOrExpression()
		{
			PluralFormsNode p = LogicalAndExpression();
			if (p == null)
				return null;
			PluralFormsNode ln = p;
			if (Token.TokenType == PluralFormsToken.Type.LogicalOr) {
				PluralFormsNode un = new PluralFormsNode(new PluralFormsToken(Token));
				if (! NextToken()) {
					return null;
				}
				p = LogicalOrExpression();
				if (p == null) {
					return null;
				}
				PluralFormsNode rn = p; // right
				if (rn.Token.TokenType == PluralFormsToken.Type.LogicalOr) {
					// see logicalAndExpression comment
					un.SetNode(0, ln);
					un.SetNode(1, rn.ReleaseNode(0));
					rn.SetNode(0, un);
					return rn;
				}
				
				un.SetNode(0, ln);
				un.SetNode(1, rn);
				return un;
			}
			return ln;
		}
		
		PluralFormsNode LogicalAndExpression()
		{
			PluralFormsNode p = EqualityExpression();
			if (p == null)
				return null;
			PluralFormsNode ln = p; // left
			if (Token.TokenType == PluralFormsToken.Type.LogicalAnd) {
				PluralFormsNode un = new PluralFormsNode(new PluralFormsToken(Token)); // up
				if (! NextToken()) {
					return null;
				}
				p = LogicalAndExpression();
				if (p == null) {
					return null;
				}
				PluralFormsNode rn = p; // right
				if (rn.Token.TokenType == PluralFormsToken.Type.LogicalAnd) {
					// transform 1 && (2 && 3) -> (1 && 2) && 3
					//     u                  r
					// l       r     ->   u      3
					//       2   3      l   2
					un.SetNode(0, ln);
					un.SetNode(1, rn.ReleaseNode(0));
					rn.SetNode(0, un);
					return rn;
				}

				un.SetNode(0, ln);
				un.SetNode(1, rn);
				return un;
			}
			return ln;
		}
		
		PluralFormsNode EqualityExpression()
		{
			PluralFormsNode p = RelationalExpression();
			if (p == null)
				return null;
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Equal || Token.TokenType == PluralFormsToken.Type.NotEqual) {
				PluralFormsNode qn = new PluralFormsNode(new PluralFormsToken(Token));
				if (! NextToken()) {
					return null;
				}
				p = RelationalExpression();
				if (p == null) {
					return null;
				}
				qn.SetNode(1, p);
				qn.SetNode(0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode MultiplicativeExpression()
		{
			PluralFormsNode p = PmExpression();
			if (p == null) {
				return null;
			}
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Reminder) {
				PluralFormsNode qn = new PluralFormsNode(new PluralFormsToken(Token));
				if (! NextToken()) {
					return null;
				}
				p = PmExpression();
				if (p == null) {
					return null;
				}
				qn.SetNode(1, p);
				qn.SetNode(0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode RelationalExpression()
		{
			PluralFormsNode p = MultiplicativeExpression();
			if (p == null)
				return null;
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Greater
			    || Token.TokenType == PluralFormsToken.Type.Less
				|| Token.TokenType == PluralFormsToken.Type.GreaterOrEqual
				|| Token.TokenType == PluralFormsToken.Type.LessOrEqual) {
				PluralFormsNode qn = new PluralFormsNode(new PluralFormsToken(Token));
				if (! NextToken()) {
					return null;
				}
				p = MultiplicativeExpression();
				if (p == null) {
					return null;
				}
				qn.SetNode(1, p);
				qn.SetNode(0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode PmExpression()
		{
			PluralFormsNode n;
			if (Token.TokenType == PluralFormsToken.Type.N || Token.TokenType == PluralFormsToken.Type.Number) {
				n = new PluralFormsNode(new PluralFormsToken(Token));
				if (! NextToken()) {
					return null;
				}
			}
			else if (Token.TokenType == PluralFormsToken.Type.LeftBracket) {
				if (! NextToken()) {
					return null;
				}
				PluralFormsNode p = Expression();
				if (p == null) {
					return null;
				}
				n = p;
				if (Token.TokenType != PluralFormsToken.Type.RightBracket) {
					return null;
				}
				if (! NextToken()) {
					return null;
				}
			}
			else {
				return null;
			}
			return n;
		}
	}
}
