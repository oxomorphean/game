"""Author C2 vector parts on one canvas; never overwrite a Krita-edited source.

The previous compact SVG supplies retained garment contours. New anatomy, hair,
eyes and alternate garments are authored Bezier geometry, without raster tracing.
"""
from pathlib import Path
from copy import deepcopy
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
NS = 'http://www.w3.org/2000/svg'
ET.register_namespace('', NS)
OLD = ET.parse(ROOT.parent / 'C/C-editable.svg').getroot()
ORIGINAL = {n.get('id'): n for n in OLD.iter() if n.get('id')}
INK = '#222c40'
HAIR, HS, HL = '#eef0ef', '#b9c2d1', '#fffcf2'
SKIN, SS = '#f5c8a5', '#dca18b'
IVORY, CS = '#f5ebd1', '#bebdaf'
NAVY, DARK = '#334561', '#222e47'
JADE, JS, GOLD = '#438e87', '#285c66', '#cba45f'
LAYERS = []


def group(name, bone, category='body', variant='base', hidden_by=()):
    layer = dict(name=name, bone=bone, category=category, variant=variant,
                 hidden_by=list(hidden_by), paths=[])
    LAYERS.append(layer)
    return layer


def path(layer, name, d, fill, stroke=None, width=3):
    a = dict(id=name, d=d, fill=fill)
    if stroke:
        a.update(stroke=stroke, **{'stroke-width': str(width)})
    layer['paths'].append(ET.Element(f'{{{NS}}}path', a))


def shape(layer, name, d, fill, width=5):
    path(layer, name, d, fill, INK, width)


def line(layer, name, d, color=INK, width=2.5):
    path(layer, name, d, 'none', color, width)


def reuse(layer, *names, omit=(), recolor=None):
    for name in names:
        node = deepcopy(ORIGINAL[name])
        nodes = list(node) if node.tag.endswith('g') else [node]
        for n in nodes:
            if n.get('id') in omit:
                continue
            for k in ['fill', 'stroke']:
                if recolor and n.get(k) in recolor:
                    n.set(k, recolor[n.get(k)])
            layer['paths'].append(n)


# All pivots use the same 1024px canvas, x right / y down.
BONES = [
    ('root', None, [548, 898]), ('pelvis', 'root', [551, 676]),
    ('chest', 'pelvis', [550, 563]), ('head', 'chest', [556, 523]),
    ('hair', 'head', [509, 230]),
    ('upper_back', 'chest', [617, 562]), ('fore_back', 'upper_back', [636, 618]),
    ('hand_back', 'fore_back', [685, 700]),
    ('upper_front', 'chest', [457, 558]), ('fore_front', 'upper_front', [422, 620]),
    ('hand_front', 'fore_front', [415, 717]),
    ('thigh_back', 'pelvis', [580, 686]), ('shin_back', 'thigh_back', [604, 758]),
    ('foot_back', 'shin_back', [597, 818]),
    ('thigh_front', 'pelvis', [520, 686]), ('shin_front', 'thigh_front', [491, 766]),
    ('foot_front', 'shin_front', [460, 838]),
    ('coat_back', 'pelvis', [608, 670]), ('coat_front', 'pelvis', [500, 666]),
]

# Rear hair is separate from the scalp, face and front fringe.
l = group('Hair_Back__Silver', 'head', 'hair', 'silver')
reuse(l, 'Hair_Back', omit=['bun-flow-1', 'bun-flow-2', 'bun-flow-3'])
for n in l['paths']:
    if n.get('fill') == '#aeb8cb': n.set('fill', HS)
    if n.get('id') == 'bun': n.set('stroke-width', '4.8')
l = group('Hair_Back__Ink', 'head', 'hair', 'ink')
shape(l, 'short-nape', 'M 425 421 Q 403 467 420 501 L 436 493 L 431 515 Q 456 519 478 507 L 493 519 L 509 492 L 488 429 Z', NAVY)
path(l, 'nape-shadow', 'M 429 460 Q 432 490 451 494 L 448 508 L 479 497 L 497 504 L 501 475 Z', DARK)

# Complete neutral body, including the regions hidden under the two outfits.
for side in ['back', 'front']:
    l = group('Body_Thigh_' + side, 'thigh_' + side, hidden_by=['classic', 'jade'])
    if side == 'front':
        shape(l, 'upper-leg', 'M 514 663 C 537 656 559 675 555 697 Q 541 737 516 778 Q 502 794 481 781 Q 463 772 473 752 L 489 703 Q 490 674 514 663 Z', SKIN)
        path(l, 'upper-leg-shade', 'M 541 676 Q 553 686 546 707 L 511 771 Q 501 785 490 779 L 510 732 Z', SS)
    else:
        shape(l, 'upper-leg', 'M 568 665 Q 595 657 611 681 L 627 745 Q 632 768 612 778 Q 588 789 578 765 L 559 700 Q 550 677 568 665 Z', SKIN)
        path(l, 'upper-leg-shade', 'M 596 676 Q 608 687 610 710 L 620 748 Q 624 769 607 774 L 594 739 Z', SS)
    l = group('Body_Shin_' + side, 'shin_' + side, hidden_by=['classic', 'jade'])
    if side == 'front':
        shape(l, 'lower-leg', 'M 481 746 Q 509 738 520 760 Q 515 790 490 831 L 482 854 Q 465 865 441 850 Q 441 832 449 813 L 465 768 Q 469 752 481 746 Z', SKIN)
        path(l, 'calf-shade', 'M 508 760 Q 503 790 478 834 L 472 851 L 447 845 Q 466 829 476 802 Z', SS)
    else:
        shape(l, 'lower-leg', 'M 596 740 Q 616 737 624 758 L 620 798 L 625 829 Q 609 842 582 830 L 581 801 L 581 762 Q 582 747 596 740 Z', SKIN)
        path(l, 'calf-shade', 'M 609 747 Q 622 750 619 772 L 611 800 L 617 827 L 600 833 L 593 810 Z', SS)

l = group('Body_Pelvis', 'pelvis', hidden_by=['classic', 'jade', 'basic'])
shape(l, 'hips', 'M 493 632 Q 549 618 607 633 L 618 677 Q 610 699 588 710 L 551 700 L 522 715 Q 495 711 483 686 Z', SKIN)
l = group('Body_Torso', 'chest', hidden_by=['classic', 'jade', 'basic'])
shape(l, 'body', 'M 524 520 L 575 520 L 581 542 Q 615 541 622 561 Q 616 591 609 614 L 610 655 Q 552 680 490 652 L 491 611 Q 478 584 480 559 Q 491 542 520 543 Z', SKIN)
path(l, 'body-shade', 'M 576 541 Q 600 549 609 564 L 599 612 L 600 651 Q 571 663 554 658 Q 582 629 570 604 Z', SS)
line(l, 'neckline', 'M 530 549 Q 552 560 576 548', '#bd8472', 2)

for side in ['back', 'front']:
    l = group('Body_Upper_' + side, 'upper_' + side, hidden_by=['classic', 'jade'])
    if side == 'front':
        shape(l, 'upper-arm', 'M 446 541 Q 467 538 478 558 Q 477 578 455 610 L 442 633 Q 430 644 415 634 Q 401 623 410 606 L 428 566 Q 433 547 446 541 Z', SKIN)
        path(l, 'arm-shade', 'M 464 551 Q 478 563 466 584 L 442 627 Q 434 639 424 632 Q 443 600 449 579 Z', SS)
    else:
        shape(l, 'upper-arm', 'M 608 542 Q 629 539 637 562 L 651 608 Q 654 628 637 634 Q 620 638 613 619 L 597 573 Q 592 551 608 542 Z', SKIN)
        path(l, 'arm-shade', 'M 629 552 L 646 607 Q 654 625 637 629 Q 631 621 630 607 L 619 575 Z', SS)
    l = group('Body_Fore_' + side, 'fore_' + side, hidden_by=['classic', 'jade'])
    if side == 'front':
        shape(l, 'forearm', 'M 413 601 Q 433 595 443 613 Q 447 633 437 658 L 428 713 Q 418 724 399 715 Q 392 704 397 689 L 401 643 Q 399 614 413 601 Z', SKIN)
        path(l, 'forearm-shade', 'M 432 614 Q 444 626 433 658 L 424 708 Q 417 716 409 714 L 418 660 Z', SS)
    else:
        shape(l, 'forearm', 'M 626 604 Q 642 600 654 617 L 683 666 L 696 696 Q 693 710 677 708 L 662 685 L 629 648 Q 615 624 626 604 Z', SKIN)
        path(l, 'forearm-shade', 'M 646 616 L 681 674 L 691 698 L 680 703 L 668 678 L 637 638 Z', SS)

# Two separate trouser designs; upper and lower parts have rounded overlap.
for variant in ['classic', 'jade']:
    for side in ['back', 'front']:
        l = group('Trouser_Upper_' + side + '__' + variant, 'thigh_' + side, 'outfit', variant)
        if side == 'front':
            shape(l, 'thigh', 'M 511 665 Q 544 661 572 687 Q 559 720 537 758 Q 519 787 489 782 Q 470 779 462 762 Q 481 725 494 691 Z', NAVY)
            path(l, 'shade', 'M 552 681 L 567 691 Q 550 733 527 764 Q 508 785 487 775 Q 516 751 530 719 Z', DARK)
            path(l, 'light', 'M 511 682 Q 527 677 539 684 Q 516 729 480 756 Q 492 719 511 682 Z', '#4b5d78')
        else:
            shape(l, 'thigh', 'M 554 672 Q 585 663 615 683 Q 625 716 641 750 Q 649 771 628 783 Q 602 792 581 775 L 560 733 Z', NAVY)
            path(l, 'shade', 'M 581 681 Q 593 708 610 736 L 635 768 Q 617 786 587 773 L 568 728 Z', DARK)
        l = group('Trouser_Lower_' + side + '__' + variant, 'shin_' + side, 'outfit', variant)
        if side == 'front':
            shape(l, 'shin', 'M 478 737 Q 508 733 536 754 Q 531 779 529 805 Q 493 830 440 821 L 428 802 Q 444 768 461 749 Z', NAVY)
            path(l, 'shade', 'M 520 750 Q 514 776 510 801 Q 481 820 443 810 L 435 801 Q 464 807 480 795 Z', DARK)
            path(l, 'fold', 'M 477 747 Q 494 741 510 747 Q 488 773 460 791 Q 468 765 477 747 Z', '#4b5d78')
            # The upper contour is hidden under the rounded thigh overlap.
            path(l, 'seam-cover', 'M 478 736 Q 510 731 536 751 L 529 762 Q 505 751 474 753 Z', NAVY)
        else:
            shape(l, 'shin', 'M 584 733 Q 612 729 632 748 L 645 777 L 637 795 Q 606 816 572 806 L 559 789 Q 571 763 576 743 Z', NAVY)
            path(l, 'shade', 'M 615 743 Q 620 766 634 779 Q 615 806 579 798 L 568 787 Q 593 790 603 772 Z', DARK)
            path(l, 'seam-cover', 'M 583 732 Q 612 728 632 744 L 635 756 Q 605 747 579 751 Z', NAVY)
        if variant == 'jade':
            for n in l['paths']:
                if n.get('fill') == NAVY: n.set('fill', '#293d52')

for side in ['back', 'front']:
    l = group('Boot_' + side, 'foot_' + side)
    reuse(l, 'Boot_' + side.title(), omit=['front-boot-crease', 'back-boot-fold'])
    for n in l['paths']:
        if n.get('id') in ['front-boot-toe', 'back-boot-toe']: n.set('fill', '#394c68')

# A simple, decent base outfit is separate from the anatomical source.
l = group('Basic_Shorts', 'pelvis', 'outfit', 'basic')
shape(l, 'shorts', 'M 491 644 Q 550 660 612 644 L 619 717 Q 595 731 568 718 L 550 689 L 532 726 Q 504 733 478 717 Z', NAVY)
path(l, 'shorts-shade', 'M 551 675 L 575 711 L 615 712 L 613 721 Q 589 734 567 720 L 550 692 L 532 727 L 502 729 Z', DARK)
line(l, 'shorts-hem', 'M 481 712 Q 505 724 530 718 M 575 713 Q 594 723 615 713', '#718292', 3)
l = group('Basic_Top', 'chest', 'outfit', 'basic')
shape(l, 'shirt', 'M 521 534 Q 543 550 576 536 L 604 542 Q 621 548 637 577 L 618 593 L 609 579 L 611 653 Q 551 673 491 651 L 484 583 L 471 599 L 438 579 Q 454 546 483 541 Z', IVORY)
path(l, 'shirt-shade', 'M 592 548 Q 605 565 604 599 L 600 648 Q 576 656 554 655 Q 580 647 581 624 L 580 559 Z', '#d1cbbc')
line(l, 'shirt-collar', 'M 520 536 Q 547 562 580 538', NAVY, 8)
line(l, 'shirt-hem', 'M 496 645 Q 547 661 606 647', '#a3a59d', 2.3)

l = group('Classic_Torso', 'chest', 'outfit', 'classic')
reuse(l, 'Torso', omit=['chest-fold'])
l = group('Classic_Upper_back', 'upper_back', 'outfit', 'classic')
shape(l, 'upper-sleeve', 'M 605 546 Q 624 540 633 561 L 647 598 Q 649 617 634 628 L 614 619 L 608 589 Z', IVORY)
path(l, 'shade', 'M 625 557 L 639 595 Q 646 611 635 622 L 621 615 L 620 583 Z', CS)
line(l, 'trim', 'M 623 595 Q 634 602 644 597', GOLD, 4)
l = group('Classic_Fore_back', 'fore_back', 'outfit', 'classic')
shape(l, 'lower-sleeve', 'M 620 605 Q 638 595 648 613 Q 656 637 675 650 L 698 670 L 683 686 Q 650 683 617 665 Q 625 638 620 605 Z', IVORY)
path(l, 'shade', 'M 629 614 L 640 613 Q 639 640 660 659 L 684 678 Q 651 677 624 663 Q 634 639 629 614 Z', CS)
line(l, 'trim', 'M 627 665 Q 659 682 687 676', GOLD, 4.5)
l = group('Classic_Upper_front', 'upper_front', 'outfit', 'classic')
reuse(l, 'front-shoulder', 'shoulder-shadow', 'shoulder-light', 'shoulder-gold')
l = group('Classic_Fore_front', 'fore_front', 'outfit', 'classic')
reuse(l, 'Sleeve_Front', omit=['front-shoulder', 'shoulder-shadow', 'shoulder-light', 'shoulder-gold', 'front-sleeve-crease', 'sleeve-rim-light'])
for side in ['back', 'front']:
    l = group('Classic_Coat_' + side, 'coat_' + side, 'outfit', 'classic')
    reuse(l, *[n.get('id') for n in ORIGINAL['Coat'] if n.get('id').startswith('coat-' + side)])
l = group('Classic_Sash', 'pelvis', 'outfit', 'classic')
reuse(l, 'navy-sash', 'sash-light')
l = group('Classic_Belt', 'pelvis', 'outfit', 'classic')
reuse(l, 'Belt')

# Second silhouette: a short jade travelling jacket, close sleeves and warm cuffs.
l = group('Jade_Torso', 'chest', 'outfit', 'jade')
shape(l, 'jacket', 'M 520 535 L 551 552 L 577 536 Q 606 534 620 554 L 623 617 L 612 658 Q 551 675 487 654 L 479 589 Q 467 558 486 545 Z', JADE)
path(l, 'jacket-shade', 'M 583 546 Q 605 555 607 583 L 610 646 Q 575 661 542 654 L 552 618 Z', JS)
path(l, 'jacket-light', 'M 486 551 Q 504 545 519 555 L 538 576 L 523 615 Q 499 598 493 581 Z', '#66a49a')
shape(l, 'inner-collar', 'M 520 536 L 551 552 L 577 536 L 569 565 L 548 587 L 528 563 Z', IVORY, 2.5)
shape(l, 'jade-lapel', 'M 579 535 L 594 544 Q 577 590 543 626 L 530 617 Q 561 583 574 550 Z', '#29434e', 3)
line(l, 'lapel-piping', 'M 584 542 Q 567 587 536 619', GOLD, 3.7)
path(l, 'waist-fold', 'M 501 624 Q 529 640 553 635 L 545 644 Q 518 642 501 624 Z', JS)
for side in ['back', 'front']:
    l = group('Jade_Upper_' + side, 'upper_' + side, 'outfit', 'jade')
    if side == 'front':
        shape(l, 'sleeve', 'M 447 539 Q 469 535 486 553 Q 482 580 458 615 L 445 632 Q 419 638 405 619 Q 411 594 427 566 Z', JADE)
        path(l, 'shade', 'M 472 549 Q 482 567 461 599 L 443 625 Q 431 630 420 622 Q 446 595 448 576 Z', JS)
        path(l, 'light', 'M 444 550 Q 456 541 467 550 Q 450 565 443 582 L 421 603 Q 427 574 444 550 Z', '#75aea1')
    else:
        shape(l, 'sleeve', 'M 604 541 Q 629 537 640 565 L 654 607 Q 651 624 632 629 L 613 615 L 600 578 Z', JADE)
        path(l, 'shade', 'M 625 552 L 649 607 Q 646 622 634 623 L 622 593 Z', JS)
    l = group('Jade_Fore_' + side, 'fore_' + side, 'outfit', 'jade')
    if side == 'front':
        shape(l, 'sleeve', 'M 409 601 Q 430 592 447 610 Q 450 630 438 659 L 435 701 Q 417 719 395 706 Q 392 675 398 645 Z', JADE)
        path(l, 'shade', 'M 431 610 Q 444 619 434 649 L 427 696 Q 418 706 406 704 L 412 658 Z', JS)
        shape(l, 'cuff', 'M 396 679 Q 415 690 435 679 L 435 702 Q 418 718 395 706 Z', '#f0e3c7', 3.5)
        line(l, 'cuff-trim', 'M 398 684 Q 414 694 433 684', GOLD, 3)
    else:
        shape(l, 'sleeve', 'M 625 604 Q 640 596 654 613 L 679 656 L 696 690 Q 692 704 676 707 Q 652 679 634 649 Q 620 627 625 604 Z', JADE)
        path(l, 'shade', 'M 644 610 L 677 664 L 690 691 L 679 701 L 660 669 Z', JS)
        shape(l, 'cuff', 'M 668 667 L 688 658 L 701 687 Q 695 703 680 706 Z', IVORY, 3.5)
        line(l, 'cuff-trim', 'M 672 673 L 693 668', GOLD, 3)
for side in ['back', 'front']:
    l = group('Jade_Coat_' + side, 'coat_' + side, 'outfit', 'jade')
    if side == 'front':
        shape(l, 'jacket-tail', 'M 490 651 L 554 665 Q 547 695 533 716 L 491 739 L 461 717 Q 471 684 490 651 Z', JADE)
        path(l, 'shade', 'M 518 670 L 547 670 Q 540 698 530 711 L 493 732 L 481 720 Z', JS)
        line(l, 'hem', 'M 467 715 L 493 734 L 531 713', GOLD, 3.5)
    else:
        shape(l, 'jacket-tail', 'M 575 659 L 613 651 Q 627 686 647 714 L 622 734 L 591 717 Z', JS)
        path(l, 'light', 'M 608 662 Q 625 696 640 711 L 625 723 Q 607 697 608 662 Z', JADE)
        line(l, 'hem', 'M 596 714 L 622 729 L 642 714', GOLD, 3.5)
l = group('Jade_Belt', 'pelvis', 'outfit', 'jade')
shape(l, 'belt', 'M 490 640 Q 551 653 611 638 L 617 659 Q 552 677 488 661 Z', '#26394a')
line(l, 'belt-piping', 'M 493 644 Q 551 658 609 643', GOLD, 3)
shape(l, 'clasp', 'M 601 642 L 617 646 L 620 657 L 609 666 L 598 655 Z', GOLD, 2.5)
path(l, 'clasp-face', 'M 603 648 L 613 649 L 615 655 L 608 660 Z', '#ead5a1')

# A neck independent of the clothing, and complete hands.
l = group('Body_Neck', 'chest')
reuse(l, 'neck', 'neck-shadow')
# Neck belongs behind collars; insert it before garment torso groups.
LAYERS.remove(l)
LAYERS.insert(next(i for i,v in enumerate(LAYERS) if v['name']=='Basic_Top'), l)
for side in ['back', 'front']:
    l = group('Hand_' + side, 'hand_' + side)
    reuse(l, 'Arm_' + side.title())

l = group('Head', 'head')
reuse(l, 'Head', omit=['face-upper-shadow'])
for n in l['paths']:
    if n.get('id') == 'head':
        n.set('stroke-width', '4.7')
        n.set('d', 'M 439 408 C 423 362 450 317 497 305 C 534 295 570 301 591 311 C 638 324 668 357 668 403 L 660 465 C 655 503 622 529 580 534 C 540 540 493 531 467 512 L 447 489 C 425 488 414 477 414 462 Q 412 443 433 438 Z')
    if n.get('id') == 'face-side-shadow': n.set('fill', '#e6af92')
# Head highlight is placed above the temple shading.
head_paths = l['paths']
bright = next(n for n in head_paths if n.get('id') == 'face-light')
head_paths.remove(bright)
head_paths.append(bright)

for variant in ['jade', 'amber']:
    l = group('Eyes__' + variant, 'head', 'eyes', variant)
    reuse(l, 'Eyes', omit=['far-lash', 'near-lash', 'near-lower-glint'])
    near = ET.Element(f'{{{NS}}}g', {'transform': 'translate(-3 -4) rotate(-8 529 486)'})
    far = ET.Element(f'{{{NS}}}g', {'transform': 'translate(-3 -4) translate(632 456) scale(1 .91) translate(-632 -456)'})
    for n in l['paths']:
        if 'near-' in n.get('id', ''): near.append(n)
        else: far.append(n)
        if n.get('id') == 'near-lid': n.set('stroke-width', '6.2')
        if n.get('id') == 'far-lid': n.set('stroke-width', '5.6')
        if variant == 'amber':
            colors={JADE:'#ae783d', JS:'#71492e', '#85c8b0':'#e4bc6b', '#90cbb3':'#ebc879'}
            if n.get('fill') in colors: n.set('fill', colors[n.get('fill')])
    l['paths']=[near,far]
l = group('Mouth', 'head')
reuse(l, 'Mouth', omit=['near-cheek', 'far-cheek', 'smile-corner'])
for n in l['paths']:
    if n.get('id') == 'smile': n.set('d', 'M 573 513 Q 590 522 607 511')

# Refined front hair: broad curved masses, tapered shadows, quiet interior lines.
def front_hair(variant):
    l = group('Hair_Front__' + variant, 'head', 'hair', variant)
    colors = (HAIR, HS, HL, '#cbd1dd') if variant=='silver' else ('#3f516b', '#29394f', '#71889a', '#50667e')
    b,s,h,m=colors
    shape(l, 'silhouette', 'M 415 273 C 441 236 491 219 536 223 Q 579 215 615 234 Q 623 220 614 208 C 641 215 657 233 662 248 Q 696 249 711 278 L 689 272 Q 704 285 713 307 Q 735 331 758 316 Q 750 343 718 355 Q 732 380 749 390 Q 733 397 713 388 Q 727 420 714 451 L 702 433 Q 695 459 678 476 L 681 448 L 660 475 L 660 441 Q 657 408 632 377 Q 617 384 608 407 Q 594 450 574 471 L 568 450 L 557 473 Q 541 456 540 433 C 507 468 473 487 443 491 L 453 465 Q 430 484 411 479 L 422 465 Q 387 482 358 470 L 377 457 Q 342 448 330 419 Q 349 428 369 418 C 344 415 338 389 350 355 Q 371 302 415 273 Z', b, 5)
    path(l, 'left-under', 'M 409 285 Q 382 315 386 341 Q 390 367 414 380 Q 395 380 384 368 Q 381 397 407 414 Q 383 425 353 422 Q 370 442 390 439 L 376 460 Q 404 465 428 440 Q 438 411 458 380 Q 425 358 425 325 Z', s)
    path(l, 'crown-turn', 'M 432 275 Q 450 250 481 240 Q 467 272 497 291 Q 522 308 553 286 Q 585 267 605 280 Q 565 291 546 314 Q 499 335 464 311 Q 441 296 432 275 Z', m)
    path(l, 'crown-light', 'M 429 270 Q 455 242 487 234 Q 471 254 486 276 Q 455 266 447 271 Q 430 284 424 301 Z', h)
    path(l, 'left-sweep-light', 'M 398 303 Q 391 338 416 358 Q 429 369 444 370 Q 421 382 402 366 Q 380 345 387 325 Z', h)
    path(l, 'side-under', 'M 655 267 Q 679 275 693 309 Q 711 337 738 337 Q 719 349 698 337 Q 700 363 727 380 Q 705 377 692 361 Q 711 397 705 429 L 694 417 Q 690 450 681 463 Q 682 419 657 390 L 635 382 Q 636 355 651 340 Q 667 311 655 267 Z', s)
    path(l, 'side-ridge', 'M 671 281 Q 692 290 699 311 Q 681 301 674 300 Q 683 325 698 337 Q 676 330 669 309 Z', h)
    path(l, 'side-ridge-two', 'M 672 346 Q 682 365 700 376 Q 682 369 679 372 Q 690 395 687 417 Q 684 390 667 373 Z', h)
    # The central lock sweeps as one form rather than two leaf-shaped pieces.
    shape(l, 'swept-fringe', 'M 604 302 C 574 302 544 320 518 347 C 488 379 467 420 433 472 Q 482 452 516 415 Q 493 458 451 487 C 490 476 521 452 545 419 Q 541 452 563 473 Q 561 449 577 423 C 600 386 623 340 613 313 Z', b, 3.2)
    path(l, 'swept-shadow', 'M 556 335 Q 523 370 497 416 Q 476 447 453 466 Q 500 442 532 395 Q 554 364 581 347 Q 568 347 552 359 Z', m)
    path(l, 'swept-light', 'M 599 311 C 573 316 548 334 529 358 Q 516 374 509 389 Q 543 363 560 362 Q 586 362 609 332 Q 612 319 599 311 Z', h)
    path(l, 'fringe-tip-shadow', 'M 608 341 Q 602 380 578 418 Q 562 443 563 459 Q 549 439 556 421 Q 582 390 596 360 Z', s)
    shape(l, 'right-fringe', 'M 614 326 Q 637 322 653 343 Q 675 372 668 421 L 661 441 Q 658 402 632 376 Q 619 380 610 400 Q 618 361 614 326 Z', b, 2.5)
    path(l, 'right-fringe-shadow', 'M 632 337 Q 654 359 659 392 L 665 419 Q 658 397 632 376 Q 621 378 614 389 Q 621 357 632 337 Z', s)
    path(l, 'right-fringe-light', 'M 621 331 Q 643 334 654 359 Q 637 346 630 348 L 621 363 Z', h)
    shape(l, 'lifted-lock', 'M 605 284 Q 626 245 616 210 Q 643 221 650 244 Q 660 272 615 296 Z', b, 3.2)
    path(l, 'lifted-shadow', 'M 631 223 Q 651 253 625 281 L 616 289 Q 639 248 631 223 Z', m)
    path(l, 'lifted-light', 'M 622 219 Q 634 232 634 245 Q 632 263 620 278 Q 633 245 622 219 Z', h)
    line(l, 'crown-flow', 'M 480 284 Q 510 307 544 286 Q 578 268 601 278', '#8996aa' if variant=='silver' else '#26374d', 1.9)
    line(l, 'temple-flow', 'M 406 297 Q 393 322 408 343', '#9ba6b8' if variant=='silver' else '#293c53', 1.7)
    return l

front_hair('silver')
front_hair('ink')
l=group('Hair_Ornament__Silver','head','hair','silver')
reuse(l,'Hair_Accessory')
l=group('Hair_Ornament__Ink','head','hair','ink')
shape(l,'short-pin','M 401 340 L 436 305 L 443 311 L 409 348 Z',GOLD,2)
path(l,'pin-light','M 406 340 L 436 310 L 438 312 L 408 344 Z','#ecd3a0')

# Open-ended contours at covered cuts: no black caps across elbows or knees.
contours={
 'Body_Fore_front':('forearm','M 409 607 Q 397 620 401 643 L 397 689 Q 392 704 399 715 M 441 615 Q 447 633 437 658 L 428 713'),
 'Body_Fore_back':('forearm','M 625 614 Q 620 629 629 648 L 662 685 L 677 708 M 648 613 L 683 666 L 696 696'),
 'Body_Shin_front':('lower-leg','M 472 757 Q 466 767 449 813 Q 441 832 441 850 M 516 757 Q 515 790 490 831 L 482 854'),
 'Body_Shin_back':('lower-leg','M 584 753 Q 581 762 581 801 L 582 830 M 620 749 Q 624 758 620 798 L 625 829'),
 'Jade_Fore_front':('sleeve','M 405 612 Q 398 632 398 645 Q 392 675 395 706 M 444 614 Q 450 630 438 659 L 435 701'),
 'Jade_Fore_back':('sleeve','M 625 614 Q 620 627 634 649 Q 652 679 676 707 M 649 607 L 679 656 L 696 690'),
 'Classic_Fore_back':('lower-sleeve','M 621 614 Q 625 638 617 665 Q 650 683 683 686 L 698 670 L 675 650 Q 656 637 648 613'),
}
for layer in LAYERS:
    name=layer['name']
    if name in contours:
        shape_id,d=contours[name]
        next(n for n in layer['paths'] if n.get('id')==shape_id).set('stroke','none')
        line(layer,'open-joint-outline',d,INK,4.5)
    if name.startswith('Trouser_Lower_'):
        next(n for n in layer['paths'] if n.get('id')=='shin').set('stroke','none')
        front='_front__' in name
        d='M 466 747 Q 444 768 428 802 L 440 821 Q 493 830 529 805 Q 531 779 536 757' if front else 'M 579 742 Q 571 763 559 789 L 572 806 Q 606 816 637 795 L 645 777 L 632 748'
        line(layer,'open-knee-outline',d,INK,4.5)
    if name=='Basic_Top':
        next(n for n in layer['paths'] if n.get('id')=='shirt').set('d','M 521 534 Q 543 550 576 536 L 604 542 Q 630 544 646 582 L 619 599 L 609 579 L 611 653 Q 551 673 491 651 L 484 583 L 465 605 L 423 585 Q 441 539 483 541 Z')


def is_visible(layer, outfit='classic', hair='silver', eyes='jade'):
    c=layer['category']
    if c=='outfit': return layer['variant']==outfit
    if c=='hair': return layer['variant']==hair
    if c=='eyes': return layer['variant']==eyes
    return outfit not in layer['hidden_by']


def element(layer, prefix='', visible=True):
    g=ET.Element(f'{{{NS}}}g',{'id':prefix+layer['name'],'data-bone':layer['bone']})
    if not visible: g.set('display','none')
    for n in layer['paths']:
        node=deepcopy(n)
        for sub in node.iter():
            if sub.get('id'): sub.set('id',prefix+layer['name']+'__'+sub.get('id'))
        g.append(node)
    return g


def document(outfit='classic',hair='silver',eyes='jade',all_layers=False):
    svg=ET.Element(f'{{{NS}}}svg',{'width':'1024','height':'1024','viewBox':'0 0 1024 1024','stroke-linecap':'round','stroke-linejoin':'round'})
    for layer in LAYERS:
        visible=is_visible(layer,outfit,hair,eyes)
        if visible or all_layers: svg.append(element(layer,visible=visible))
    return ET.tostring(svg,encoding='unicode')+'\n'


if __name__=='__main__':
    ROOT.mkdir(exist_ok=True)
    (ROOT/'parts').mkdir(exist_ok=True)
    for l in LAYERS:
        doc=ET.Element(f'{{{NS}}}svg',{'width':'1024','height':'1024','viewBox':'0 0 1024 1024','stroke-linecap':'round','stroke-linejoin':'round'})
        doc.append(element(l))
        (ROOT/'parts'/(l['name']+'.svg')).write_text(ET.tostring(doc,encoding='unicode')+'\n')
    (ROOT/'C2-source.svg').write_text(document(all_layers=True))
    for outfit in ['classic','jade','basic']:
        for hair in ['silver','ink']:
            (ROOT/(outfit+'-'+hair+'.svg')).write_text(document(outfit,hair))
    no_hair=ET.fromstring(document('basic'))
    for node in list(no_hair):
        if node.get('id','').startswith('Hair_'):no_hair.remove(node)
    (ROOT/'body-basic.svg').write_text(ET.tostring(no_hair,encoding='unicode')+'\n')
    manifest={'body_id':'NGUTHU_C2_34_V1','canvas':[1024,1024],'view':'three-quarter-right',
              'root':[548,898],'default':{'outfit':'classic','hair':'silver','eyes':'jade'},
              'bones':[{'name':n,'parent':p,'position':xy} for n,p,xy in BONES],
              'layers':[{k:v for k,v in l.items() if k!='paths'}|{'source':'parts/'+l['name']+'.svg','default_visible':is_visible(l),'order':i} for i,l in enumerate(LAYERS)],
              'variants':{'outfit':['classic','jade','basic'],'hair':['silver','ink'],'eyes':['jade','amber']}}
    (ROOT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    print(f'Authored {len(LAYERS)} layers; common 1024px canvas, {len(BONES)} bind pivots; no path quota.')
