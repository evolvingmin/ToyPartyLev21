/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;

namespace SeawispHunter.MinibufferConsole {

[AttributeUsage(AttributeTargets.Field)]
public class Description : Attribute {
  public string text;
  /* Description */
  public Description(string text) {
    this.text = text;
  }
}
}
