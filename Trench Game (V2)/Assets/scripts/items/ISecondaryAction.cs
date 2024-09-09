using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISecondaryAction
{
    public void SecondaryAction();

    public abstract string SecondaryVerb {get;}
}
