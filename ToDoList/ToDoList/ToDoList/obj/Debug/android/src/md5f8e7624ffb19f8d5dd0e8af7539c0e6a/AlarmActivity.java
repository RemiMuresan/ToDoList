package md5f8e7624ffb19f8d5dd0e8af7539c0e6a;


public class AlarmActivity
	extends android.app.Activity
	implements
		mono.android.IGCUserPeer
{
	static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreate:(Landroid/os/Bundle;)V:GetOnCreate_Landroid_os_Bundle_Handler\n" +
			"";
		mono.android.Runtime.register ("ToDoList.AlarmActivity, ToDoList, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", AlarmActivity.class, __md_methods);
	}


	public AlarmActivity () throws java.lang.Throwable
	{
		super ();
		if (getClass () == AlarmActivity.class)
			mono.android.TypeManager.Activate ("ToDoList.AlarmActivity, ToDoList, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onCreate (android.os.Bundle p0)
	{
		n_onCreate (p0);
	}

	private native void n_onCreate (android.os.Bundle p0);

	java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
