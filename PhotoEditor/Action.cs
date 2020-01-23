using System;

namespace PhotoEditor
{
	class Action
	{
		public string action { get; private set; }
		public string[] parameters { get; private set; }

		public Action(string p_action, string[] p_parameters)
		{
			action = p_action;
			parameters = p_parameters;
		}

		public string GetFullAction()
		{
			return action + "(" + String.Join(",", parameters) + ");";
		}
	}
}
