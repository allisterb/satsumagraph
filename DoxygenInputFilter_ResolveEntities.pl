#!/usr/bin/env perl

# This is an input filter for Doxygen which resolves all XML entities.

# In Satsuma's documentation comments, XML entities are used everywhere, like &lt; instead of <.
# This allows the .NET documentation XML generator to work. Otherwise we would get malformed XML warning.
# However, Doxygen cannot always resolve entities.
# Examples include the \code environment and references like Prim&lt;TCost&gt;.
# Thus we need to filter the input for Doxygen by resolving entities.

$fn = $ARGV[0];
open(F, $fn);
for $line (<F>)
{
	$i = index($line, '//');
	if ($i >= 0)
	{
		$comment = substr($line, $i);

		$comment =~ s/&lt\;/</g;
		$comment =~ s/&gt\;/>/g;
		$comment =~ s/&amp\;/&/g;
		$comment =~ s/&quot\;/"/g;
		$comment =~ s/&apos\;/'/g;
		
		$line = substr($line, 0, $i) . $comment;
	}
	print $line;
}
